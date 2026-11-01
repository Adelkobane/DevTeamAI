namespace DevTeam.Web.Services;

/// <summary>
/// Förmedlar ett färdigt underlag (t.ex. en AI-genererad blueprint) från en komponent till kontaktformuläret.
/// Formuläret lyssnar på <see cref="Requested"/> och fyller i meddelandefältet.
/// </summary>
public sealed class ContactPrefill
{
    /// <summary>Ett underlag har begärts. Formuläret fyller i det och visar en notis.</summary>
    public event Action<string>? Requested;

    /// <summary>Underlaget som väntar om formuläret ännu inte lyssnade när det begärdes.</summary>
    public string? Pending { get; private set; }

    public void Request(string message)
    {
        Pending = message;
        Requested?.Invoke(message);
    }

    /// <summary>Anropas av formuläret när det har tagit emot underlaget.</summary>
    public void Consume() => Pending = null;
}
