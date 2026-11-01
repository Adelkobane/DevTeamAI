using DevTeam.Api.Models;
using DevTeam.Api.Services.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DevTeam.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController(IContactEmailSender emailSender, ILogger<ContactController> logger) : ControllerBase
{
    /// <summary>Tar emot ett kontaktmeddelande från portföljsidan och skickar det vidare via e-post.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Contact)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] ContactMessage message, CancellationToken cancellationToken)
    {
        // [ApiController] returnerar automatiskt 400 om valideringen (DataAnnotations) misslyckas.

        try
        {
            await emailSender.SendAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Klienten avbröt anropet – inget att göra.
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            // Läcker inga detaljer (SMTP-värd, inloggning m.m.) till klienten.
            logger.LogError(ex, "Kunde inte skicka kontaktmeddelandet.");
            return Problem(
                title: "Meddelandet kunde inte skickas just nu.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Accepted(new { status = "received" });
    }
}
