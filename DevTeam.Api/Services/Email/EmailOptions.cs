namespace DevTeam.Api.Services.Email;

/// <summary>Konfiguration för utskick av kontaktmeddelanden (sektionen "Email" i appsettings).</summary>
public class EmailOptions
{
    /// <summary>"Log" (standard, skickar inget) eller "Smtp".</summary>
    public string Provider { get; set; } = "Log";

    /// <summary>Avsändaradress (måste oftast vara en adress som SMTP-kontot får skicka från).</summary>
    public string From { get; set; } = string.Empty;

    public string FromName { get; set; } = "DevTeam Portfolio";

    /// <summary>Mottagare av kontaktmeddelanden, t.ex. din egen adress.</summary>
    public string To { get; set; } = string.Empty;

    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;

    /// <summary>true = STARTTLS (port 587). Sätt false och Port 465 för implicit TLS.</summary>
    public bool UseStartTls { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    /// <summary>Lägg aldrig i appsettings.json – använd user-secrets eller miljövariabler.</summary>
    public string Password { get; set; } = string.Empty;
}
