using DevTeam.Api.Models;

namespace DevTeam.Api.Services.Email;

/// <summary>
/// Skickar vidare ett kontaktmeddelande. Implementera detta interface för att lägga till fler leverantörer
/// (t.ex. Azure Communication Services) och registrera det i Program.cs.
/// </summary>
public interface IContactEmailSender
{
    Task SendAsync(ContactMessage message, CancellationToken cancellationToken);
}
