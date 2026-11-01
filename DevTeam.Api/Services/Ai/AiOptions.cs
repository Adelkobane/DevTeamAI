namespace DevTeam.Api.Services.Ai;

/// <summary>Konfiguration under "Ai" i appsettings.json. Nycklar läggs i user-secrets eller miljövariabler, aldrig i filen.</summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>None (AI av) | OpenAI | Groq | AzureOpenAI</summary>
    public string Provider { get; set; } = "None";

    /// <summary>Max antal tokens i ett svar (kostnadsskydd).</summary>
    public int MaxOutputTokens { get; set; } = 1000;

    /// <summary>Låg temperatur = mer faktabaserade svar.</summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>Tidsgräns för anropet till AI-leverantören tills svaret börjar komma.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Hur många tecken kunskapsbas-kontext (RAG) som högst skickas med per fråga.</summary>
    public int ContextCharBudget { get; set; } = 8000;

    public OpenAiCompatibleOptions OpenAI { get; set; } = new()
    {
        BaseUrl = "https://api.openai.com/v1",
        Model = "gpt-4o-mini"
    };

    /// <summary>
    /// Groq har ett OpenAI-kompatibelt API (snabb, gratisnivå). Vilka modeller som finns varierar per konto och
    /// ändras över tid – lista dem med GET https://api.groq.com/openai/v1/models. gpt-oss är resonemangsmodeller som
    /// stöder "reasoning_effort"; byter du till en modell som inte gör det (t.ex. Llama) – töm ReasoningEffort.
    /// </summary>
    public OpenAiCompatibleOptions Groq { get; set; } = new()
    {
        BaseUrl = "https://api.groq.com/openai/v1",
        Model = "openai/gpt-oss-120b",
        ReasoningEffort = "low"
    };

    public AzureOpenAiOptions AzureOpenAI { get; set; } = new();
}

public sealed class OpenAiCompatibleOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Valfritt: "low" | "medium" | "high" för resonemangsmodeller (t.ex. gpt-oss). Tomt = skickas inte
    /// (krävs för vanliga modeller, som annars avvisar parametern). Lågt värde ger snabbare svar och lämnar
    /// mer av tokenbudgeten till själva svaret.
    /// </summary>
    public string ReasoningEffort { get; set; } = string.Empty;
}

public sealed class AzureOpenAiOptions
{
    /// <summary>T.ex. https://min-resurs.openai.azure.com</summary>
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Namnet på modell-deploymenten i Azure (inte modellnamnet).</summary>
    public string Deployment { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "2024-10-21";
}
