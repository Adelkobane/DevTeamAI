namespace DevTeam.Web.Models;

// Svaret från POST /api/ai/generate-spec (matchar API:ets AppSpec).

public sealed class SpecField
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public sealed class SpecRelation
{
    public string Target { get; set; } = string.Empty;

    /// <summary>"1:1", "1:N", "N:1" eller "N:M".</summary>
    public string Cardinality { get; set; } = string.Empty;
}

public sealed class SpecEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SpecField> Fields { get; set; } = [];
    public List<SpecRelation> Relations { get; set; } = [];
}

public sealed class SpecScreen
{
    public string Name { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public List<string> Components { get; set; } = [];
}

public sealed class SpecSprint
{
    public string Name { get; set; } = string.Empty;
    public int Weeks { get; set; }
    public List<string> Goals { get; set; } = [];
}

public sealed class AppSpec
{
    public string Title { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> UserStories { get; set; } = [];
    public List<string> TechStack { get; set; } = [];
    public List<SpecEntity> Entities { get; set; } = [];
    public List<SpecScreen> UiComponents { get; set; } = [];
    public List<SpecSprint> EstimatedTimeline { get; set; } = [];

    /// <summary>Färdig HTML-förhandsvisning (renderas i en sandlådad iframe).</summary>
    public string PreviewHtml { get; set; } = string.Empty;

    /// <summary>Summan av sprintarnas veckor.</summary>
    public int TotalWeeks => EstimatedTimeline.Sum(s => s.Weeks);
}
