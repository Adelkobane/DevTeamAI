namespace DevTeam.Api.Services.Ai;

/// <summary>Ett meddelande i en chat-konversation. Role: "system", "user" eller "assistant".</summary>
public sealed record AiMessage(string Role, string Content);

/// <summary>Leverantörsoberoende förfrågan till en språkmodell.</summary>
/// <param name="JsonMode">Be modellen svara med ett giltigt JSON-objekt (används för jobbmatchningen).</param>
public sealed record AiRequest(
    IReadOnlyList<AiMessage> Messages,
    int? MaxTokens = null,
    double? Temperature = null,
    bool JsonMode = false);

/// <summary>
/// Abstraktion över språkmodell-leverantören (OpenAI, Groq, Azure OpenAI ...). Leverantör väljs med
/// "Ai:Provider" i konfigurationen; resten av koden pratar bara med det här gränssnittet.
/// </summary>
public interface IAiService
{
    /// <summary>False när "Ai:Provider" är "None" – då svarar AI-endpointsen 503.</summary>
    bool IsConfigured { get; }

    string ProviderName { get; }

    /// <summary>Strömmar svaret i textdelar allteftersom modellen genererar dem.</summary>
    IAsyncEnumerable<string> StreamAsync(AiRequest request, CancellationToken cancellationToken = default);

    /// <summary>Hämtar hela svaret på en gång.</summary>
    Task<string> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Fel från AI-leverantören. Innehåller aldrig nycklar eller råa svar som kan visas för klienten.</summary>
public sealed class AiServiceException : Exception
{
    public AiServiceException(string message, int? upstreamStatusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        UpstreamStatusCode = upstreamStatusCode;
    }

    /// <summary>HTTP-status från leverantören, om anropet nådde fram.</summary>
    public int? UpstreamStatusCode { get; }

    /// <summary>True om leverantören begränsar oss (429) eller är tillfälligt otillgänglig (5xx).</summary>
    public bool IsTransient => UpstreamStatusCode is 429 or >= 500;
}
