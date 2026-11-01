using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace DevTeam.Api.Services.Ai;

/// <summary>Vart ett chat-completions-anrop ska skickas och hur det autentiseras.</summary>
/// <param name="Model">Modellnamn i förfrågan. Null för Azure OpenAI (där styr deploymenten i URL:en).</param>
public sealed record ChatCompletionsTarget(
    string ProviderName,
    Uri Url,
    string? Model,
    string AuthHeaderName,
    string AuthHeaderValue,
    string? ReasoningEffort = null);

/// <summary>
/// <see cref="IAiService"/> för alla leverantörer som talar OpenAI:s chat-completions-protokoll:
/// OpenAI, Groq (Llama 3 m.fl.) och Azure OpenAI. Skillnaden mellan dem är bara URL, autentisering och modellfält,
/// vilket beskrivs av <see cref="ChatCompletionsTarget"/>.
/// </summary>
public sealed class ChatCompletionsAiService : IAiService
{
    private readonly HttpClient _http;
    private readonly ChatCompletionsTarget _target;
    private readonly AiOptions _options;
    private readonly ILogger<ChatCompletionsAiService> _logger;

    public ChatCompletionsAiService(
        HttpClient http,
        ChatCompletionsTarget target,
        Microsoft.Extensions.Options.IOptions<AiOptions> options,
        ILogger<ChatCompletionsAiService> logger)
    {
        _http = http;
        _target = target;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => true;

    public string ProviderName => _target.ProviderName;

    public async Task<string> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(request, stream: false, cancellationToken);

        try
        {
            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);

            if (document.RootElement.TryGetProperty("choices", out var choices) &&
                choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(content.GetString()))
            {
                return content.GetString()!;
            }
        }
        catch (JsonException ex)
        {
            throw new AiServiceException("AI-leverantören returnerade ett ogiltigt svar.", inner: ex);
        }

        throw new AiServiceException("AI-leverantören returnerade ett tomt svar.");
    }

    public async IAsyncEnumerable<string> StreamAsync(
        AiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(request, stream: true, cancellationToken);
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(body, Encoding.UTF8);

        // Server-Sent Events från leverantören: rader som "data: {json}" och slutligen "data: [DONE]".
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var data = line[5..].Trim();
            if (data.Length == 0) continue;
            if (data == "[DONE]") yield break;

            var delta = TryReadDelta(data);
            if (!string.IsNullOrEmpty(delta)) yield return delta;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(AiRequest request, bool stream, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, _target.Url)
        {
            Content = new StringContent(BuildBody(request, stream), Encoding.UTF8, "application/json")
        };
        message.Headers.TryAddWithoutValidation(_target.AuthHeaderName, _target.AuthHeaderValue);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(stream ? "text/event-stream" : "application/json"));

        HttpResponseMessage response;
        try
        {
            // ResponseHeadersRead: returnera så fort headers kommit, så att strömmen kan läsas löpande.
            response = await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new AiServiceException("Kunde inte nå AI-leverantören.", inner: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiServiceException("AI-leverantören svarade inte i tid.", inner: ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            // Svarskroppen loggas (avkortad) men skickas aldrig vidare till klienten.
            var snippet = await ReadSnippetAsync(response, cancellationToken);
            response.Dispose();

            _logger.LogWarning("AI-leverantören {Provider} svarade {Status}: {Body}", _target.ProviderName, status, snippet);
            throw new AiServiceException($"AI-leverantören svarade med status {status}.", status);
        }

        return response;
    }

    private string BuildBody(AiRequest request, bool stream)
    {
        var body = new Dictionary<string, object?>
        {
            ["messages"] = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            ["temperature"] = request.Temperature ?? _options.Temperature,
            ["max_tokens"] = request.MaxTokens ?? _options.MaxOutputTokens,
            ["stream"] = stream
        };

        if (_target.Model is not null) body["model"] = _target.Model;
        if (!string.IsNullOrWhiteSpace(_target.ReasoningEffort)) body["reasoning_effort"] = _target.ReasoningEffort;
        if (request.JsonMode) body["response_format"] = new { type = "json_object" };

        return JsonSerializer.Serialize(body);
    }

    private static string? TryReadDelta(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            // Azure skickar ibland en första händelse med tom "choices" (innehållsfilter) – hoppa över den.
            if (document.RootElement.TryGetProperty("choices", out var choices) &&
                choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }
        }
        catch (JsonException)
        {
            // Ofullständig eller okänd händelse – ignorera.
        }

        return null;
    }

    private static async Task<string> ReadSnippetAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            return text.Length <= 300 ? text : text[..300] + "…";
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            return "(kunde inte läsa svarskroppen)";
        }
    }
}
