using DevTeam.Api.Services.Ai;
using Microsoft.AspNetCore.Mvc;

namespace DevTeam.Api.Controllers;

/// <summary>Svar från <c>GET /api/status</c>. Innehåller inga hemligheter eller leverantörsdetaljer.</summary>
public sealed record SystemStatus(bool Api, bool Azure, bool Ai);

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    // Miljövariabler som Azure sätter i driftmiljön: App Service (WEBSITE_*) och Container Apps (CONTAINER_APP_NAME).
    private static readonly string[] AzureMarkers = ["WEBSITE_SITE_NAME", "WEBSITE_INSTANCE_ID", "CONTAINER_APP_NAME"];

    private readonly IAiService _ai;

    public StatusController(IAiService ai) => _ai = ai;

    /// <summary>
    /// Ärlig systemstatus för statusraden på startsidan:
    /// <c>api</c> = API:t svarar, <c>azure</c> = körs i Azure (inte lokalt), <c>ai</c> = en AI-leverantör är konfigurerad.
    /// </summary>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult<SystemStatus> Get()
    {
        var onAzure = AzureMarkers.Any(name => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)));
        return Ok(new SystemStatus(Api: true, Azure: onAzure, Ai: _ai.IsConfigured));
    }
}
