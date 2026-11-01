namespace DevTeam.Api;

public static class RateLimitPolicies
{
    /// <summary>Spamskydd för POST /api/contact.</summary>
    public const string Contact = "contact";

    /// <summary>Delas av POST /api/ai/chat och POST /api/ai/match-job (per IP) – skyddar AI-nyckeln mot missbruk.</summary>
    public const string Ai = "ai";
}
