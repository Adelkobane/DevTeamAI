using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DevTeam.Web;
using DevTeam.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API:ets adress. HTTPS-profilen: https://localhost:7172, HTTP-profilen: http://localhost:5017.
// Kan åsidosättas via "ApiBaseUrl" i wwwroot/appsettings.json (t.ex. för produktion).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7172";

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// DevTeam AI: strömmande chatt och jobbmatchning mot API:et.
builder.Services.AddScoped<AiClient>();

// Skickar ett färdigt underlag (AI-blueprinten) till kontaktformuläret.
builder.Services.AddScoped<ContactPrefill>();

// Lokalisering (IStringLocalizer<Strings>). Kulturen (sv/en) läses från localStorage i index.html och
// skickas till Blazor via Blazor.start({ applicationCulture }), som även laddar rätt satellit-assemblies.
builder.Services.AddLocalization();

await builder.Build().RunAsync();
