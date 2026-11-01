using System.Text;
using DevTeam.Api.Models;
using DevTeam.Api.Services.Portfolio;

namespace DevTeam.Api.Services.Ai;

/// <summary>Ett sökbart stycke kunskap ur portföljen.</summary>
/// <param name="Always">Tas alltid med i kontexten (översikt och "vad vi inte vet").</param>
public sealed record KnowledgeChunk(string Id, string Title, string Text, bool Always = false);

/// <summary>
/// Kunskapsbasen för RAG (retrieval-augmented generation). Innehållet byggs från <see cref="PortfolioContent"/>
/// – samma källa som sidan visar – och delas upp i stycken som kan rangordnas mot frågan.
/// </summary>
public interface IPortfolioKnowledge
{
    IReadOnlyList<KnowledgeChunk> GetChunks(string language);

    /// <summary>
    /// Bygger kontexten för en fråga: obligatoriska stycken först, därefter övriga i fallande relevans
    /// tills <paramref name="maxChars"/> nås.
    /// </summary>
    string BuildContext(string language, string query, int maxChars);

    /// <summary>All kunskap (används vid jobbmatchning där hela profilen behövs).</summary>
    string BuildFullContext(string language);
}

public sealed class PortfolioKnowledge : IPortfolioKnowledge
{
    // Vanliga ord som inte bidrar till relevansen (svenska + engelska).
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "och", "att", "det", "som", "en", "ett", "är", "av", "för", "på", "med", "till", "om", "har", "vad", "hur", "vem",
        "kan", "ni", "du", "de", "den", "jag", "vi", "inte", "berätta", "mer", "the", "and", "what", "how", "who", "can",
        "you", "your", "are", "is", "of", "to", "in", "on", "for", "with", "about", "tell", "me", "more", "do", "does", "a"
    };

    // Kunskapen är oföränderlig under processens livstid, så den byggs en gång per språk.
    private readonly Lazy<IReadOnlyList<KnowledgeChunk>> _sv = new(() => Build(Languages.Swedish));
    private readonly Lazy<IReadOnlyList<KnowledgeChunk>> _en = new(() => Build(Languages.English));

    public IReadOnlyList<KnowledgeChunk> GetChunks(string language) =>
        Languages.Normalize(language) == Languages.English ? _en.Value : _sv.Value;

    public string BuildFullContext(string language) =>
        Render(GetChunks(language));

    public string BuildContext(string language, string query, int maxChars)
    {
        var chunks = GetChunks(language);
        var queryTokens = Tokenize(query);

        var selected = new List<KnowledgeChunk>();
        var used = 0;

        // 1) Obligatoriska stycken.
        foreach (var chunk in chunks.Where(c => c.Always))
        {
            selected.Add(chunk);
            used += chunk.Text.Length + chunk.Title.Length;
        }

        // 2) Övriga stycken efter relevans (stabil sortering: jämna poäng behåller ursprungsordningen).
        var ranked = chunks
            .Where(c => !c.Always)
            .Select((chunk, index) => (chunk, index, score: Score(chunk, queryTokens)))
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.index);

        foreach (var (chunk, _, _) in ranked)
        {
            var size = chunk.Text.Length + chunk.Title.Length;
            if (used + size > maxChars) continue; // hoppa över för stora, prova nästa
            selected.Add(chunk);
            used += size;
        }

        // Behåll kunskapsbasens ordning i kontexten så att den blir läsbar.
        return Render(chunks.Where(selected.Contains));
    }

    private static IReadOnlyList<KnowledgeChunk> Build(string language)
    {
        var sv = language == Languages.Swedish;
        var chunks = new List<KnowledgeChunk>();

        // Översikt (alltid med)
        var overview = new StringBuilder();
        foreach (var fact in PortfolioContent.TeamFacts) overview.AppendLine("- " + fact.Get(language));
        chunks.Add(new KnowledgeChunk("overview", sv ? "Studioöversikt" : "Studio overview", overview.ToString().Trim(), Always: true));

        // Det vi inte vet (alltid med – hindrar gissningar)
        chunks.Add(new KnowledgeChunk(
            "limits", sv ? "Saknad information" : "Missing information",
            PortfolioContent.KnownUnknowns.Get(language), Always: true));

        // Profiler
        foreach (var p in PortfolioContent.Profiles)
        {
            var text = new StringBuilder();
            text.AppendLine($"{(sv ? "Namn" : "Name")}: {p.Name}");
            text.AppendLine($"{(sv ? "Roll" : "Role")}: {p.Title}");
            text.AppendLine($"{(sv ? "Ort" : "Location")}: {p.Location}");
            text.AppendLine($"{(sv ? "Biografi" : "Bio")}: {p.Bio.Get(language)}");
            text.AppendLine($"Frontend: {string.Join(", ", p.FrontendSkills)}");
            text.AppendLine($"Backend: {string.Join(", ", p.BackendSkills)}");
            text.AppendLine($"Cloud & DevOps: {string.Join(", ", p.CloudSkills)}");
            text.AppendLine($"{(sv ? "Kvalitet och arbetssätt" : "Quality and ways of working")}: {string.Join(", ", p.Methodologies)}");
            // Kontaktuppgifterna visas redan öppet på sidan. Ordet "kontakt/contact" gör att frågor om hur man når oss hittar hit.
            if (!string.IsNullOrWhiteSpace(p.Email)) text.AppendLine($"{(sv ? "Kontakt e-post" : "Contact email")}: {p.Email}");
            if (!string.IsNullOrWhiteSpace(p.Phone)) text.AppendLine($"{(sv ? "Kontakt telefon" : "Contact phone")}: {p.Phone}");
            if (!string.IsNullOrWhiteSpace(p.GitHubUrl)) text.AppendLine($"GitHub: {p.GitHubUrl}");
            if (!string.IsNullOrWhiteSpace(p.LinkedInUrl)) text.AppendLine($"LinkedIn: {p.LinkedInUrl}");

            chunks.Add(new KnowledgeChunk($"profile-{p.Id}", $"{(sv ? "Profil" : "Profile")}: {p.Name}", text.ToString().Trim()));
        }

        // Projekt
        foreach (var project in PortfolioContent.Projects)
        {
            var text = new StringBuilder();
            text.AppendLine($"{(sv ? "Projekt" : "Project")}: {project.Title}");
            text.AppendLine($"{(sv ? "Beskrivning" : "Description")}: {project.Description.Get(language)}");
            text.AppendLine($"{(sv ? "Teknik" : "Tech stack")}: {string.Join(", ", project.TechStack)}");
            if (!string.IsNullOrWhiteSpace(project.GitHubUrl)) text.AppendLine($"GitHub: {project.GitHubUrl}");
            if (!string.IsNullOrWhiteSpace(project.LiveDemoUrl)) text.AppendLine($"Live demo: {project.LiveDemoUrl}");

            chunks.Add(new KnowledgeChunk($"project-{project.Title.ToLowerInvariant()}", $"{(sv ? "Projekt" : "Project")}: {project.Title}", text.ToString().Trim()));
        }

        return chunks;
    }

    private static string Render(IEnumerable<KnowledgeChunk> chunks)
    {
        var sb = new StringBuilder();
        foreach (var chunk in chunks)
        {
            sb.AppendLine($"## {chunk.Title}");
            sb.AppendLine(chunk.Text);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Enkel lexikal relevans: antal frågetermer som förekommer, med extra vikt för träff i rubriken.</summary>
    private static int Score(KnowledgeChunk chunk, HashSet<string> queryTokens)
    {
        if (queryTokens.Count == 0) return 0;

        var title = chunk.Title.ToLowerInvariant();
        var text = chunk.Text.ToLowerInvariant();
        var id = chunk.Id.ToLowerInvariant();
        var score = 0;

        foreach (var token in queryTokens)
        {
            if (title.Contains(token, StringComparison.Ordinal) || id.Contains(token, StringComparison.Ordinal)) score += 5;
            if (text.Contains(token, StringComparison.Ordinal)) score += 1;
        }

        return score;
    }

    private static HashSet<string> Tokenize(string text)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length >= 3)
            {
                var token = current.ToString();
                if (!StopWords.Contains(token)) tokens.Add(Stem(token));
            }
            current.Clear();
        }

        foreach (var ch in text.ToLowerInvariant())
        {
            // Bokstäver och siffror (+ tecken som är vanliga i teknikord, t.ex. c#, .net)
            if (char.IsLetterOrDigit(ch) || ch is '#' or '+') current.Append(ch);
            else Flush();
        }
        Flush();

        return tokens;
    }

    // Grov normalisering av genitiv/plural ("adels" -> "adel", "projekten" -> "projekt") så att böjda former matchar.
    private static string Stem(string token)
    {
        foreach (var suffix in new[] { "ens", "ets", "en", "et", "er", "ar", "s" })
        {
            if (token.Length > suffix.Length + 2 && token.EndsWith(suffix, StringComparison.Ordinal))
                return token[..^suffix.Length];
        }
        return token;
    }
}
