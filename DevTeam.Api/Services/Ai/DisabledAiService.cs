namespace DevTeam.Api.Services.Ai;

/// <summary>Används när "Ai:Provider" är "None": AI-endpointsen svarar då 503 i stället för att krascha.</summary>
public sealed class DisabledAiService : IAiService
{
    public bool IsConfigured => false;

    public string ProviderName => "None";

    public IAsyncEnumerable<string> StreamAsync(AiRequest request, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("AI är inte konfigurerat (Ai:Provider = None).");

    public Task<string> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("AI är inte konfigurerat (Ai:Provider = None).");
}
