namespace DevTeam.Web.Models;

/// <summary>En tidigare tur som skickas med som konversationshistorik. Role: "user" eller "assistant".</summary>
public sealed record ChatTurn(string Role, string Content);

public sealed class JobMatchResult
{
    /// <summary>Matchningspoäng 0–100.</summary>
    public int Score { get; set; }

    public List<string> Strengths { get; set; } = [];

    public List<string> Gaps { get; set; } = [];

    public string Reasoning { get; set; } = string.Empty;
}
