using System.ComponentModel.DataAnnotations;

namespace DevTeam.Api.Models;

public class ContactMessage
{
    [Required(ErrorMessage = "Namn är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Namnet får vara högst 100 tecken.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-post är obligatoriskt.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    [StringLength(200, ErrorMessage = "E-postadressen får vara högst 200 tecken.")]
    public string Email { get; set; } = string.Empty;

    public const int MaxMessageLength = 60_000;

    [Required(ErrorMessage = "Meddelande är obligatoriskt.")]
    [StringLength(MaxMessageLength, MinimumLength = 10, ErrorMessage = "Meddelandet måste vara mellan 10 och 60000 tecken.")]
    public string Message { get; set; } = string.Empty;
}
