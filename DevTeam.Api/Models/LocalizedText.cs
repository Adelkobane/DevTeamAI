namespace DevTeam.Api.Models;

/// <summary>Stödda språk och normalisering av språkkoder från klienten.</summary>
public static class Languages
{
    public const string Swedish = "sv";
    public const string English = "en";

    /// <summary>Svenska är standard om språket saknas eller inte stöds.</summary>
    public static string Normalize(string? lang) =>
        lang is not null && lang.Trim().StartsWith(English, StringComparison.OrdinalIgnoreCase)
            ? English
            : Swedish;
}

/// <summary>En text på både svenska och engelska.</summary>
public sealed record LocalizedText(string Sv, string En)
{
    /// <summary>Returnerar texten på angivet språk (faller tillbaka på svenska).</summary>
    public string Get(string lang) =>
        Languages.Normalize(lang) == Languages.English && !string.IsNullOrWhiteSpace(En) ? En : Sv;
}
