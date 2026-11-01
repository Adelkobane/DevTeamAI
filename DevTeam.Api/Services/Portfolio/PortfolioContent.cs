using DevTeam.Api.Models;

namespace DevTeam.Api.Services.Portfolio;

/// <summary>Lagrad profil med texter på båda språken.</summary>
public sealed record ProfileSource(
    string Id,
    string Name,
    string Title,
    string Location,
    LocalizedText Bio,
    List<string> FrontendSkills,
    List<string> BackendSkills,
    List<string> CloudSkills,
    List<string> Methodologies,
    string Email,
    string Phone,
    string GitHubUrl,
    string LinkedInUrl);

/// <summary>Lagrat projekt med beskrivning på båda språken.</summary>
public sealed record ProjectSource(
    string Title,
    LocalizedText Description,
    List<string> TechStack,
    string GitHubUrl,
    string LiveDemoUrl,
    string ImageUrl,
    bool Featured = false);

/// <summary>
/// Enda källan till portföljens innehåll. Används av ProfilesController/ProjectsController (det som visas på sidan)
/// och av AI-kunskapsbasen (RAG) – så att AI-assistenten alltid säger samma sak som sidan.
/// </summary>
public static class PortfolioContent
{
    private static List<string> FrontendStack() => ["HTML", "CSS", "JavaScript", "React", "Blazor WebAssembly"];
    private static List<string> BackendStack() => ["Modern .NET", "C#", "ASP.NET Core Web API", "EF Core", "SQL / PostgreSQL", "AI Integration"];
    private static List<string> CloudStack() => ["Cloud-Native", "Azure", "Docker"];
    private static List<string> Ways() => ["Agile QA"];

    public static IReadOnlyList<ProfileSource> Profiles { get; } =
    [
        new ProfileSource(
            Id: "adel",
            Name: "Adel Kobansson",
            Title: "Lead AI & Solutions Engineer · Founder",
            Location: "Stockholm",
            Bio: new LocalizedText(
                Sv: "Grundare och Lead AI & Solutions Engineer bakom DevTeam AI i Stockholm. Du beskriver din idé – " +
                    "Adel bygger och levererar applikationen från grunden med Modern .NET, C# och ASP.NET Core Web API, " +
                    "med Blazor WebAssembly eller React i frontend. Integrerar AI-funktioner (chatt, RAG, agenter och textanalys) " +
                    "i verkliga produkter och levererar Cloud-Native lösningar med EF Core mot PostgreSQL/SQL, Docker och Azure. " +
                    "Kvalitet är en naturlig del av leveransen genom Agile QA.",
                En: "Founder and Lead AI & Solutions Engineer behind DevTeam AI in Stockholm. You describe your idea – " +
                    "Adel builds and delivers the application from the ground up with Modern .NET, C# and ASP.NET Core Web API, " +
                    "with Blazor WebAssembly or React on the frontend. Integrates AI features (chat, RAG, agents and text analysis) " +
                    "into real products and delivers Cloud-Native solutions with EF Core against PostgreSQL/SQL, Docker and Azure. " +
                    "Quality is a natural part of delivery through Agile QA."),
            FrontendSkills: FrontendStack(),
            BackendSkills: BackendStack(),
            CloudSkills: CloudStack(),
            Methodologies: Ways(),
            Email: "Adelkobane@outlook.com",
            Phone: "0737297647",
            GitHubUrl: "https://github.com/Adelkobane",
            LinkedInUrl: "https://www.linkedin.com/in/adel-kobane-334b92188")
    ];

    // Tomma länkar döljs i gränssnittet, och tom ImageUrl ger en designad platshållare.
    public static IReadOnlyList<ProjectSource> Projects { get; } =
    [
        new ProjectSource(
            Title: "DevTeam AI AppBuilder",
            Description: new LocalizedText(
                Sv: "Den centrala produkten i DevTeam AI. Du beskriver din idé – vi bygger och levererar din applikation " +
                    "från grunden med hjälp av modern .NET och AI. Kunden får på sekunder en komplett projektspecifikation: " +
                    "användarflöden, datamodell med SQL-schema, skärmskisser, tech stack och tidsplan i leveransfaser. " +
                    "Byggd med Modern .NET, Blazor WebAssembly och AI-integration, och används som startpunkt för varje kundprojekt.",
                En: "The core product of DevTeam AI. You describe your idea – we build and deliver your application " +
                    "from the ground up with modern .NET and AI. Within seconds the client receives a complete project blueprint: " +
                    "user flows, data model with SQL schema, screen sketches, tech stack and a delivery-phase timeline. " +
                    "Built with Modern .NET, Blazor WebAssembly and AI integration, and used as the starting point for every client project."),
            TechStack: [".NET", "Blazor WebAssembly", "AI Integration", "ASP.NET Core Web API", "PostgreSQL"],
            GitHubUrl: "",
            LiveDemoUrl: "",
            ImageUrl: "",
            Featured: true)
    ];

    /// <summary>
    /// Övergripande fakta om studion. Håll dem kortfattade och sanna – AI-assistenten får inte påstå mer än så här.
    /// </summary>
    public static IReadOnlyList<LocalizedText> TeamFacts { get; } =
    [
        new LocalizedText(
            Sv: "Adel Kobansson är grundare och Lead AI & Solutions Engineer bakom DevTeam AI, en AI-utvecklingsstudio i Stockholm.",
            En: "Adel Kobansson is the founder and Lead AI & Solutions Engineer behind DevTeam AI, an AI development studio in Stockholm."),
        new LocalizedText(
            Sv: "Värdeerbjudandet: du beskriver din idé – vi bygger och levererar din applikation från grunden med hjälp av modern .NET och AI.",
            En: "Value proposition: you describe your idea – we build and deliver your application from the ground up with modern .NET and AI."),
        new LocalizedText(
            Sv: "Studion erbjuder skräddarsydd systemutveckling, AI-integrationer och molnlösningar (Cloud-Native) – från idé och prototyp till produktion.",
            En: "The studio offers custom software development, AI integrations and Cloud-Native cloud solutions – from idea and prototype to production."),
        new LocalizedText(
            Sv: "Adel tar emot nya kundprojekt i Stockholm. På webbplatsen kan en kund göra om en appidé till en projektspecifikation med DevTeam AI AppBuilder.",
            En: "Adel is taking on new client projects in Stockholm. On the website a client can turn an app idea into a project blueprint with DevTeam AI AppBuilder."),
        new LocalizedText(
            Sv: "Den egna produkten är DevTeam AI AppBuilder, en AI-driven arkitektur- och prototypmotor som är startpunkten för kundprojekt.",
            En: "The own product is DevTeam AI AppBuilder, an AI-driven architecture and prototype engine that is the starting point for client projects."),
        new LocalizedText(
            Sv: "Kontakt sker via kontaktformuläret längst ned på webbplatsen, via e-post till Adel eller via länkarna i profilen.",
            En: "Contact is via the contact form at the bottom of this website, email to Adel, or the links on the profile.")
    ];

    /// <summary>
    /// Sådant som inte finns i underlaget. Assistenten ska säga att det saknas i stället för att gissa.
    /// </summary>
    public static LocalizedText KnownUnknowns { get; } = new(
        Sv: "Följande finns INTE i underlaget och får inte gissas: antal år av erfarenhet, tidigare arbetsgivare och uppdragsgivare, " +
            "utbildning och certifieringar, löneanspråk, tillgänglighet/startdatum, språkkunskaper utöver svenska och engelska på webbplatsen, " +
            "och mätbara resultat från projekten. Det finns ingen medgrundare i underlaget – studion drivs av Adel Kobansson.",
        En: "The following is NOT in the source material and must not be guessed: years of experience, previous employers and clients, " +
            "education and certifications, salary expectations, availability/start date, language skills beyond the Swedish and English of the website, " +
            "and measurable project results. There is no co-founder in the source material – the studio is run by Adel Kobansson.");
}
