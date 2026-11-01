namespace DevTeam.Api.Services.Ai;

/// <summary>Översätter "Ai:*"-konfigurationen till ett <see cref="ChatCompletionsTarget"/> för vald leverantör.</summary>
public static class AiProviderFactory
{
    /// <returns>Null när AI är avstängt (<c>Ai:Provider = None</c>).</returns>
    /// <exception cref="InvalidOperationException">Leverantören är okänd eller konfigurationen ofullständig (misslyckas vid uppstart).</exception>
    public static ChatCompletionsTarget? CreateTarget(AiOptions options)
    {
        var provider = options.Provider.Trim();

        if (provider.Length == 0 || provider.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return FromOpenAiCompatible("OpenAI", options.OpenAI);
        }

        if (provider.Equals("Groq", StringComparison.OrdinalIgnoreCase))
        {
            return FromOpenAiCompatible("Groq", options.Groq);
        }

        if (provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var azure = options.AzureOpenAI;

            if (string.IsNullOrWhiteSpace(azure.Endpoint) ||
                string.IsNullOrWhiteSpace(azure.ApiKey) ||
                string.IsNullOrWhiteSpace(azure.Deployment))
            {
                throw new InvalidOperationException(
                    "Ai:Provider är 'AzureOpenAI' men Ai:AzureOpenAI:Endpoint, ApiKey och Deployment måste anges.");
            }

            var url = new Uri(
                $"{azure.Endpoint.TrimEnd('/')}/openai/deployments/{Uri.EscapeDataString(azure.Deployment)}" +
                $"/chat/completions?api-version={Uri.EscapeDataString(azure.ApiVersion)}");

            // Azure väljer modell via deploymenten i URL:en och autentiserar med headern "api-key".
            return new ChatCompletionsTarget("AzureOpenAI", url, Model: null, "api-key", azure.ApiKey);
        }

        throw new InvalidOperationException(
            $"Okänd Ai:Provider '{options.Provider}'. Giltiga värden: None, OpenAI, Groq, AzureOpenAI.");
    }

    private static ChatCompletionsTarget FromOpenAiCompatible(string name, OpenAiCompatibleOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey) ||
            string.IsNullOrWhiteSpace(options.Model) ||
            string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException(
                $"Ai:Provider är '{name}' men Ai:{name}:ApiKey, Model och BaseUrl måste anges " +
                $"(lägg nyckeln i user-secrets eller miljövariabeln Ai__{name}__ApiKey).");
        }

        var url = new Uri($"{options.BaseUrl.TrimEnd('/')}/chat/completions");
        var effort = string.IsNullOrWhiteSpace(options.ReasoningEffort) ? null : options.ReasoningEffort.Trim();
        return new ChatCompletionsTarget(name, url, options.Model, "Authorization", $"Bearer {options.ApiKey}", effort);
    }
}
