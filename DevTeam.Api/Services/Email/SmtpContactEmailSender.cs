using DevTeam.Api.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DevTeam.Api.Services.Email;

public class SmtpContactEmailSender(IOptions<EmailOptions> options, ILogger<SmtpContactEmailSender> logger)
    : IContactEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(ContactMessage message, CancellationToken cancellationToken)
    {
        var mime = BuildMessage(message);
        var smtp = _options.Smtp;

        using var client = new SmtpClient { Timeout = 15_000 };

        var socketOptions = smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("Kontaktmeddelande skickat via SMTP.");
    }

    private MimeMessage BuildMessage(ContactMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.From));
        mime.To.Add(MailboxAddress.Parse(_options.To));

        var safeName = SanitizeHeaderValue(message.Name);

        // Reply-To = besökarens adress, så att man kan svara direkt. TryParse skyddar mot ogiltiga adresser.
        if (MailboxAddress.TryParse(message.Email.Trim(), out var replyTo))
        {
            replyTo.Name = safeName;
            mime.ReplyTo.Add(replyTo);
        }

        mime.Subject = $"Nytt meddelande från portföljen: {safeName}";

        mime.Body = new TextPart("plain")
        {
            Text = $"Namn: {safeName}\r\nE-post: {SanitizeHeaderValue(message.Email)}\r\n\r\n{message.Message}"
        };

        return mime;
    }

    /// <summary>Tar bort radbrytningar/kontrolltecken (förhindrar header-injection) och kortar ner.</summary>
    private static string SanitizeHeaderValue(string value)
    {
        var cleaned = new string(value.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return cleaned.Length > 100 ? cleaned[..100] : cleaned;
    }
}
