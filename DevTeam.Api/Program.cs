using System.Threading.RateLimiting;
using DevTeam.Api;
using DevTeam.Api.Services.Ai;
using DevTeam.Api.Services.Email;
using Microsoft.AspNetCore.RateLimiting;

const string BlazorCorsPolicy = "BlazorClient";

var builder = WebApplication.CreateBuilder(args);

// --- CORS ---------------------------------------------------------------------------------------
// Tillåtna origins för Blazor WebAssembly-appen (konfigureras i appsettings.json under "Cors:AllowedOrigins").
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["https://localhost:7132", "http://localhost:5282"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(BlazorCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              // Så att Blazor kan läsa Retry-After från 429-svar och visa hur länge besökaren ska vänta.
              .WithExposedHeaders("Retry-After"));
});

// --- Rate limiting (spamskydd) --------------------------------------------------------------------
// Standard: max 3 kontaktmeddelanden per minut och IP-adress. Kan ändras under "RateLimiting:Contact".
var contactPermitLimit = builder.Configuration.GetValue("RateLimiting:Contact:PermitLimit", 3);
var contactWindowSeconds = builder.Configuration.GetValue("RateLimiting:Contact:WindowSeconds", 60);

// AI-endpointsen (chatt + jobbmatchning) delar en gräns per IP: standard max 5 anrop per minut ("RateLimiting:Ai").
var aiPermitLimit = builder.Configuration.GetValue("RateLimiting:Ai:PermitLimit", 5);
var aiWindowSeconds = builder.Configuration.GetValue("RateLimiting:Ai:WindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = (context, _) =>
    {
        var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : contactWindowSeconds;

        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        return ValueTask.CompletedTask;
    };

    options.AddPolicy(RateLimitPolicies.Contact, httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            // Bakom en proxy/lastbalanserare måste forwarded headers vara aktiverade för att
            // RemoteIpAddress ska vara klientens riktiga IP (se ASPNETCORE_FORWARDEDHEADERS_ENABLED).
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = contactPermitLimit,
                Window = TimeSpan.FromSeconds(contactWindowSeconds),
                SegmentsPerWindow = 6,
                QueueLimit = 0 // avvisa direkt i stället för att köa
            }));

    options.AddPolicy(RateLimitPolicies.Ai, httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = aiPermitLimit,
                Window = TimeSpan.FromSeconds(aiWindowSeconds),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            }));
});

// --- AI-assistent (DevTeam AI) -------------------------------------------------------------------
// Leverantör väljs med "Ai:Provider": None | OpenAI | Groq | AzureOpenAI. Nycklar ska ligga i user-secrets
// eller miljövariabler (t.ex. Ai__Groq__ApiKey), aldrig i appsettings.json.
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
var aiOptions = builder.Configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

builder.Services.AddSingleton<IPortfolioKnowledge, PortfolioKnowledge>();
builder.Services.AddScoped<IPortfolioAssistant, PortfolioAssistant>();
builder.Services.AddScoped<IAppSpecGenerator, AppSpecGenerator>();

var aiTarget = AiProviderFactory.CreateTarget(aiOptions);
if (aiTarget is null)
{
    builder.Services.AddSingleton<IAiService, DisabledAiService>();
}
else
{
    builder.Services.AddSingleton(aiTarget);
    builder.Services.AddHttpClient<IAiService, ChatCompletionsAiService>(client =>
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, aiOptions.TimeoutSeconds)));
}

// --- E-post för kontaktformuläret -----------------------------------------------------------------
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();

if (emailOptions.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
{
    // Misslyckas direkt vid uppstart i stället för vid första meddelandet om konfigurationen är ofullständig.
    if (string.IsNullOrWhiteSpace(emailOptions.Smtp.Host) ||
        string.IsNullOrWhiteSpace(emailOptions.From) ||
        string.IsNullOrWhiteSpace(emailOptions.To))
    {
        throw new InvalidOperationException(
            "Email:Provider är 'Smtp' men Email:From, Email:To och Email:Smtp:Host måste anges.");
    }

    builder.Services.AddSingleton<IContactEmailSender, SmtpContactEmailSender>();
}
else
{
    builder.Services.AddSingleton<IContactEmailSender, LoggingContactEmailSender>();
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// CORS före rate limiter, så att även 429-svar får CORS-headers och webbläsaren kan läsa dem.
app.UseCors(BlazorCorsPolicy);

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();
