using DevTeam.Api.Models;

namespace DevTeam.Api.Services.Email;

/// <summary>
/// Används när ingen e-postleverantör är konfigurerad (t.ex. lokalt). Skickar inget – loggar bara
/// att ett meddelande kom in. Innehållet loggas inte eftersom det är personuppgifter.
/// </summary>
public class LoggingContactEmailSender(ILogger<LoggingContactEmailSender> logger) : IContactEmailSender
{
    public Task SendAsync(ContactMessage message, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Kontaktmeddelande mottaget ({Length} tecken) men ingen e-postleverantör är konfigurerad " +
            "(Email:Provider = \"Log\"). Sätt Email:Provider till \"Smtp\" för att skicka e-post.",
            message.Message.Length);

        return Task.CompletedTask;
    }
}
