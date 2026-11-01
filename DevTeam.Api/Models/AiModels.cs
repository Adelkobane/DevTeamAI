using System.ComponentModel.DataAnnotations;

namespace DevTeam.Api.Models;

/// <summary>En tidigare tur i konversationen (skickas av klienten, behandlas som otillförlitlig indata).</summary>
public class ChatTurn
{
    /// <summary>"user" eller "assistant". Systemmeddelanden accepteras aldrig från klienten.</summary>
    [Required]
    [RegularExpression("^(user|assistant)$", ErrorMessage = "Role must be 'user' or 'assistant'.")]
    public string Role { get; set; } = "user";

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;
}

public class ChatRequest
{
    public const int MaxMessageLength = 1000;
    public const int MaxHistoryTurns = 12;

    [Required]
    [StringLength(MaxMessageLength, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Tidigare tur(er), äldst först. Äldre än <see cref="MaxHistoryTurns"/> avvisas.</summary>
    [MaxLength(MaxHistoryTurns)]
    public List<ChatTurn> History { get; set; } = [];

    /// <summary>Gränssnittets språk ("sv" eller "en"). Svar ges på användarens språk, med detta som standard.</summary>
    public string? Language { get; set; }
}

public class MatchJobRequest
{
    public const int MinLength = 40;
    public const int MaxLength = 6000;

    [Required]
    [StringLength(MaxLength, MinimumLength = MinLength)]
    public string JobDescription { get; set; } = string.Empty;

    /// <summary>Språket analysen ska skrivas på ("sv" eller "en").</summary>
    public string? Language { get; set; }
}

public class JobMatchResult
{
    /// <summary>Matchningspoäng 0–100.</summary>
    public int Score { get; set; }

    /// <summary>Adels nyckelstyrkor i relation till rollen.</summary>
    public List<string> Strengths { get; set; } = [];

    /// <summary>Krav som saknas eller inte kan bekräftas utifrån profilen.</summary>
    public List<string> Gaps { get; set; } = [];

    /// <summary>Kort motivering av poängen.</summary>
    public string Reasoning { get; set; } = string.Empty;
}

// ------------------------------------------------------------------------------------------------
// AI Blueprint & App Generator: kundens idé in, en strukturerad projektspecifikation ut.
// ------------------------------------------------------------------------------------------------

public class GenerateSpecRequest
{
    public const int MinLength = 10;
    public const int MaxLength = 1500;

    /// <summary>Kundens appidé i fritext. Behandlas som otillförlitlig indata.</summary>
    [Required]
    [StringLength(MaxLength, MinimumLength = MinLength)]
    public string Idea { get; set; } = string.Empty;

    /// <summary>Språket specifikationen ska skrivas på ("sv" eller "en").</summary>
    public string? Language { get; set; }
}

/// <summary>Ett fält i en databasmodell.</summary>
public class SpecField
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Datatyp, t.ex. "uuid", "string", "decimal", "datetime".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Kort notering, t.ex. "PK", "FK", "unik" eller "valfri". Kan vara tom.</summary>
    public string Note { get; set; } = string.Empty;
}

/// <summary>En relation från en entitet till en annan.</summary>
public class SpecRelation
{
    /// <summary>Namnet på den entitet relationen pekar på.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>"1:1", "1:N", "N:1" eller "N:M".</summary>
    public string Cardinality { get; set; } = string.Empty;
}

/// <summary>En databasmodell (tabell) med fält och relationer.</summary>
public class SpecEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SpecField> Fields { get; set; } = [];
    public List<SpecRelation> Relations { get; set; } = [];
}

/// <summary>En huvudskärm/vy i appen med de UI-komponenter den består av.</summary>
public class SpecScreen
{
    public string Name { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public List<string> Components { get; set; } = [];
}

/// <summary>En sprint/milstolpe i den uppskattade tidsplanen.</summary>
public class SpecSprint
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Uppskattad längd i veckor (1–12).</summary>
    public int Weeks { get; set; }

    public List<string> Goals { get; set; } = [];
}

/// <summary>Den genererade projektspecifikationen (blueprint).</summary>
public class AppSpec
{
    public string Title { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> UserStories { get; set; } = [];
    public List<string> TechStack { get; set; } = [];
    public List<SpecEntity> Entities { get; set; } = [];
    public List<SpecScreen> UiComponents { get; set; } = [];
    public List<SpecSprint> EstimatedTimeline { get; set; } = [];

    /// <summary>Färdig HTML-förhandsvisning av kundens startsida (Tailwind-klasser, utan egna script).</summary>
    public string PreviewHtml { get; set; } = string.Empty;
}

/// <summary>En ändring av en redan genererad HTML-prototyp.</summary>
public class UpdatePreviewRequest
{
    public const int MinHtmlLength = 20;
    public const int MaxHtmlLength = 48_000;
    public const int MinInstructionLength = 3;
    public const int MaxInstructionLength = 400;

    /// <summary>Nuvarande prototyp (otillförlitlig HTML). Saneras innan den skickas till modellen.</summary>
    [Required]
    [StringLength(MaxHtmlLength, MinimumLength = MinHtmlLength)]
    public string Html { get; set; } = string.Empty;

    /// <summary>Kundens ändring, t.ex. "Byt till mörkt tema".</summary>
    [Required]
    [StringLength(MaxInstructionLength, MinimumLength = MinInstructionLength)]
    public string Instruction { get; set; } = string.Empty;

    /// <summary>Språket ny text ska skrivas på ("sv" eller "en").</summary>
    public string? Language { get; set; }
}

/// <summary>Den uppdaterade, sanerade HTML-prototypen.</summary>
public class UpdatePreviewResponse
{
    public string PreviewHtml { get; set; } = string.Empty;
}
