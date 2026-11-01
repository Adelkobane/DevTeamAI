using DevTeam.Api.Models;
using DevTeam.Api.Services.Portfolio;
using Microsoft.AspNetCore.Mvc;

namespace DevTeam.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    /// <summary>
    /// Returnerar grundarens profil. Biografin returneras på språket i <paramref name="lang"/>
    /// ("sv" eller "en"; svenska är standard). Innehållet ligger i <see cref="PortfolioContent"/>.
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<DeveloperProfile>> GetProfiles([FromQuery] string? lang = null)
    {
        var language = Languages.Normalize(lang);

        var profiles = PortfolioContent.Profiles.Select(s => new DeveloperProfile
        {
            Id = s.Id,
            Name = s.Name,
            Title = s.Title,
            Location = s.Location,
            Bio = s.Bio.Get(language),
            FrontendSkills = [.. s.FrontendSkills],
            BackendSkills = [.. s.BackendSkills],
            CloudSkills = [.. s.CloudSkills],
            Methodologies = [.. s.Methodologies],
            Email = s.Email,
            Phone = s.Phone,
            GitHubUrl = s.GitHubUrl,
            LinkedInUrl = s.LinkedInUrl
        });

        return Ok(profiles);
    }
}
