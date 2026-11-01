using System.ComponentModel.DataAnnotations;

namespace DevTeam.Web.Models;

/// <summary>
/// ErrorMessage innehåller resursnycklar (se Resources/Strings*.resx) som översätts vid visning
/// av komponenten LocalizedValidationMessage. Gränserna måste matcha API:ets ContactMessage.
/// </summary>
public class ContactMessage
{
    [Required(ErrorMessage = "Validation.NameRequired")]
    [StringLength(100, ErrorMessage = "Validation.NameLength")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation.EmailRequired")]
    [EmailAddress(ErrorMessage = "Validation.EmailInvalid")]
    [StringLength(200, ErrorMessage = "Validation.EmailLength")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Måste matcha API:ets gräns. Rymmer specifikation plus den genererade HTML-prototypen.</summary>
    public const int MaxMessageLength = 60_000;

    [Required(ErrorMessage = "Validation.MessageRequired")]
    [StringLength(MaxMessageLength, MinimumLength = 10, ErrorMessage = "Validation.MessageLength")]
    public string Message { get; set; } = string.Empty;
}
