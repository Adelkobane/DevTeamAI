using System.Text.Json;
using DevTeam.Api.Models;
using DevTeam.Api.Services.Ai;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DevTeam.Api.Controllers;

/// <summary>
/// DevTeam AI: en RAG-assistent om Adel och studion, och en jobbmatchare. Båda endpointsen delar rate limit-policyn
/// <see cref="RateLimitPolicies.Ai"/> (per IP) så att ingen kan missbruka API-nyckeln.
/// </summary>
[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private const long MaxBodyBytes = 32 * 1024;

    private static readonly JsonSerializerOptions SseJson = new(JsonSerializerDefaults.Web);

    private readonly IPortfolioAssistant _assistant;
    private readonly IAppSpecGenerator _specGenerator;
    private readonly IAiService _ai;
    private readonly ILogger<AiController> _logger;

    public AiController(IPortfolioAssistant assistant, IAppSpecGenerator specGenerator, IAiService ai, ILogger<AiController> logger)
    {
        _assistant = assistant;
        _specGenerator = specGenerator;
        _ai = ai;
        _logger = logger;
    }

    /// <summary>
    /// Strömmande chatt (Server-Sent Events). Händelser:
    /// <c>data: {"delta":"text"}</c> för varje textdel, <c>event: done</c> när svaret är klart och
    /// <c>event: error</c> (<c>{"code":"..."}</c>) om något går fel mitt i strömmen.
    /// </summary>
    [HttpPost("chat")]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (!_ai.IsConfigured)
        {
            await Unavailable().ExecuteResultAsync(ControllerContext);
            return;
        }

        // Loggar bara storlek, aldrig innehållet i besökarens fråga.
        _logger.LogInformation("AI-chatt: fråga på {Length} tecken, {Turns} tidigare tur(er)", request.Message.Length, request.History.Count);

        await using var stream = _assistant.StreamChatAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken);

        // Hämta första textdelen INNAN strömmen startas: då kan leverantörsfel fortfarande ge en riktig HTTP-status
        // (efter att headers skickats går det inte att ändra statuskoden).
        bool hasFirst;
        try
        {
            hasFirst = await stream.MoveNextAsync();
        }
        catch (OperationCanceledException)
        {
            return; // klienten avbröt
        }
        catch (AiServiceException ex)
        {
            await ProviderError(ex).ExecuteResultAsync(ControllerContext);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers["X-Accel-Buffering"] = "no"; // stäng av buffring i nginx-liknande proxyer
        await Response.StartAsync(cancellationToken);

        try
        {
            while (hasFirst)
            {
                await WriteEventAsync(null, new { delta = stream.Current }, cancellationToken);
                hasFirst = await stream.MoveNextAsync();
            }

            await WriteEventAsync("done", new { }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Klienten stängde fliken eller tryckte på Stopp – inget att göra.
        }
        catch (AiServiceException ex)
        {
            _logger.LogWarning(ex, "AI-strömmen avbröts av leverantören");
            await TryWriteErrorAsync(ex.IsTransient ? "unavailable" : "provider_error");
        }
    }

    /// <summary>Analyserar en jobbannons mot Adels profil och returnerar poäng, styrkor, luckor och motivering.</summary>
    [HttpPost("match-job")]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<IActionResult> MatchJob([FromBody] MatchJobRequest request, CancellationToken cancellationToken)
    {
        if (!_ai.IsConfigured) return Unavailable();

        _logger.LogInformation("AI-jobbmatchning: annons på {Length} tecken", request.JobDescription.Length);

        try
        {
            var result = await _assistant.MatchJobAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (AiServiceException ex)
        {
            return ProviderError(ex);
        }
    }

    /// <summary>
    /// AI Blueprint &amp; App Generator: analyserar kundens appidé och returnerar en strukturerad projektspecifikation
    /// (titel, användarberättelser, teknikstack, databasmodeller, skärmar och en indikativ tidsplan).
    /// Svarar 422 om texten inte beskriver en app.
    /// </summary>
    [HttpPost("generate-spec")]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<IActionResult> GenerateSpec([FromBody] GenerateSpecRequest request, CancellationToken cancellationToken)
    {
        if (!_ai.IsConfigured) return Unavailable();

        // Loggar bara storlek, aldrig kundens idé.
        _logger.LogInformation("AI-blueprint: idé på {Length} tecken", request.Idea.Length);

        try
        {
            var spec = await _specGenerator.GenerateAsync(request, cancellationToken);
            return Ok(spec);
        }
        catch (InvalidAppIdeaException)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Texten beskriver ingen appidé.",
                detail: "Beskriv appen eller tjänsten du vill bygga, till exempel vem den är till för och vad den ska göra.");
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (AiServiceException ex)
        {
            return ProviderError(ex);
        }
    }

    /// <summary>
    /// Skriver om en befintlig HTML-prototyp efter kundens instruktion. Sidan byts ut i klienten utan omladdning.
    /// Delar rate limit med övriga AI-anrop.
    /// </summary>
    [HttpPost("update-preview")]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    [RequestSizeLimit(128 * 1024)]
    public async Task<IActionResult> UpdatePreview([FromBody] UpdatePreviewRequest request, CancellationToken cancellationToken)
    {
        if (!_ai.IsConfigured) return Unavailable();

        _logger.LogInformation(
            "AI-prototyp: uppdatering, html {HtmlLength} tecken, instruktion {InstructionLength} tecken",
            request.Html.Length, request.Instruction.Length);

        try
        {
            var html = await _specGenerator.UpdatePreviewAsync(request, cancellationToken);
            return Ok(new UpdatePreviewResponse { PreviewHtml = html });
        }
        catch (InvalidPreviewException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Förhandsvisningen kunde inte uppdateras.",
                detail: "HTML-koden saknas eller är för kort.");
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (AiServiceException ex)
        {
            return ProviderError(ex);
        }
    }

    // --------------------------------------------------------------- Hjälpmetoder

    private async Task WriteEventAsync(string? eventName, object payload, CancellationToken cancellationToken)
    {
        // JSON-kodning gör att radbrytningar i texten inte förstör SSE-formatet.
        var frame = (eventName is null ? "" : $"event: {eventName}\n") +
                    $"data: {JsonSerializer.Serialize(payload, SseJson)}\n\n";

        await Response.WriteAsync(frame, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private async Task TryWriteErrorAsync(string code)
    {
        try
        {
            await WriteEventAsync("error", new { code }, HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException)
        {
            // Klienten har redan lämnat.
        }
    }

    private IActionResult Unavailable() => Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "AI-assistenten är inte aktiverad.",
        detail: "Ange Ai:Provider och API-nyckel i konfigurationen för att aktivera den.");

    // Klienten får aldrig se leverantörens svar, nycklar eller interna fel – bara en generell status.
    private IActionResult ProviderError(AiServiceException ex)
    {
        _logger.LogWarning(ex, "AI-leverantören misslyckades (upstream-status {Status})", ex.UpstreamStatusCode);

        return ex.IsTransient || ex.UpstreamStatusCode is null
            ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "AI-tjänsten är tillfälligt otillgänglig.")
            : Problem(statusCode: StatusCodes.Status502BadGateway, title: "AI-tjänsten kunde inte slutföra begäran.");
    }
}
