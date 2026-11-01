using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DevTeam.Web.Models;

namespace DevTeam.Web.Services;

public enum AiErrorKind
{
    /// <summary>429: för många anrop (max 5/min per IP).</summary>
    RateLimited,

    /// <summary>503: AI är avstängt eller leverantören otillgänglig.</summary>
    Unavailable,

    /// <summary>400: ogiltig indata.</summary>
    BadRequest,

    /// <summary>422: texten beskriver ingen appidé (AI Blueprint).</summary>
    NotAnIdea,

    /// <summary>Övriga serverfel (t.ex. 502).</summary>
    Failed,

    /// <summary>Nätverksfel eller avbruten ström.</summary>
    Network
}

public sealed class AiClientException : Exception
{
    public AiClientException(AiErrorKind kind, int? retryAfterSeconds = null, Exception? inner = null)
        : base($"AI-anrop misslyckades: {kind}", inner)
    {
        Kind = kind;
        RetryAfterSeconds = retryAfterSeconds;
    }

    public AiErrorKind Kind { get; }

    /// <summary>Väntetid i sekunder från Retry-After vid <see cref="AiErrorKind.RateLimited"/>.</summary>
    public int? RetryAfterSeconds { get; }
}

/// <summary>Klient för DevTeam AI: strömmande chatt (Server-Sent Events), jobbmatchning och blueprint-generatorn.</summary>
public sealed class AiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public AiClient(HttpClient http) => _http = http;

    /// <summary>Strömmar svaret i textdelar. Kastar <see cref="AiClientException"/> vid fel.</summary>
    public async IAsyncEnumerable<string> StreamChatAsync(
        string message,
        IReadOnlyList<ChatTurn> history,
        string language,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/ai/chat")
        {
            Content = JsonContent.Create(new { message, history, language }, options: Json)
        };

        // ResponseHeadersRead: ge oss svaret så fort headers kommit, så att texten kan visas medan den genereras.
        using var response = await SendAsync(
            () => _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken));

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? eventName = null;
        var completed = false;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Length == 0)
            {
                eventName = null; // tom rad avslutar en händelse
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventName = line[6..].Trim();
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            if (eventName == "done")
            {
                completed = true;
                break;
            }

            if (eventName == "error")
            {
                throw new AiClientException(AiErrorKind.Unavailable);
            }

            var delta = TryReadDelta(line[5..].Trim());
            if (!string.IsNullOrEmpty(delta)) yield return delta;
        }

        // Strömmen tog slut utan "done": anslutningen bröts mitt i svaret.
        if (!completed) throw new AiClientException(AiErrorKind.Network);
    }

    /// <summary>Analyserar en jobbannons mot teamets profil.</summary>
    public async Task<JobMatchResult> MatchJobAsync(string jobDescription, string language, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/ai/match-job")
        {
            Content = JsonContent.Create(new { jobDescription, language }, options: Json)
        };

        using var response = await SendAsync(() => _http.SendAsync(request, cancellationToken));

        try
        {
            return await response.Content.ReadFromJsonAsync<JobMatchResult>(Json, cancellationToken)
                   ?? throw new AiClientException(AiErrorKind.Failed);
        }
        catch (JsonException ex)
        {
            throw new AiClientException(AiErrorKind.Failed, inner: ex);
        }
    }

    /// <summary>Gör en appidé till en strukturerad projektspecifikation (blueprint).</summary>
    public async Task<AppSpec> GenerateSpecAsync(string idea, string language, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/ai/generate-spec")
        {
            Content = JsonContent.Create(new { idea, language }, options: Json)
        };

        using var response = await SendAsync(() => _http.SendAsync(request, cancellationToken));

        try
        {
            return await response.Content.ReadFromJsonAsync<AppSpec>(Json, cancellationToken)
                   ?? throw new AiClientException(AiErrorKind.Failed);
        }
        catch (JsonException ex)
        {
            throw new AiClientException(AiErrorKind.Failed, inner: ex);
        }
    }

    /// <summary>Skriver om en befintlig HTML-prototyp och returnerar den sanerade sidan.</summary>
    public async Task<string> UpdatePreviewAsync(string html, string instruction, string language, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/ai/update-preview")
        {
            Content = JsonContent.Create(new { html, instruction, language }, options: Json)
        };

        using var response = await SendAsync(() => _http.SendAsync(request, cancellationToken));

        try
        {
            var result = await response.Content.ReadFromJsonAsync<UpdatePreviewResponse>(Json, cancellationToken)
                         ?? throw new AiClientException(AiErrorKind.Failed);
            if (string.IsNullOrWhiteSpace(result.PreviewHtml))
                throw new AiClientException(AiErrorKind.Failed);
            return result.PreviewHtml;
        }
        catch (JsonException ex)
        {
            throw new AiClientException(AiErrorKind.Failed, inner: ex);
        }
    }

    private sealed class UpdatePreviewResponse
    {
        public string PreviewHtml { get; set; } = string.Empty;
    }

    private static async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (HttpRequestException ex)
        {
            throw new AiClientException(AiErrorKind.Network, inner: ex);
        }

        if (response.IsSuccessStatusCode) return response;

        var status = response.StatusCode;
        var retryAfter = response.Headers.RetryAfter?.Delta is { } delta ? (int?)Math.Ceiling(delta.TotalSeconds) : null;
        response.Dispose();

        throw status switch
        {
            HttpStatusCode.TooManyRequests => new AiClientException(AiErrorKind.RateLimited, retryAfter),
            HttpStatusCode.ServiceUnavailable => new AiClientException(AiErrorKind.Unavailable),
            HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge => new AiClientException(AiErrorKind.BadRequest),
            HttpStatusCode.UnprocessableEntity => new AiClientException(AiErrorKind.NotAnIdea),
            _ => new AiClientException(AiErrorKind.Failed)
        };
    }

    private static string? TryReadDelta(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.String
                ? delta.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
