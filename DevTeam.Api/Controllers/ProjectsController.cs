using DevTeam.Api.Models;
using DevTeam.Api.Services.Portfolio;
using Microsoft.AspNetCore.Mvc;

namespace DevTeam.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    /// <summary>
    /// Returnerar produktkorten (DevTeam AI AppBuilder). Beskrivningen returneras på språket i
    /// <paramref name="lang"/> ("sv" eller "en"; svenska är standard). Innehållet ligger i <see cref="PortfolioContent"/>.
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<Project>> GetProjects([FromQuery] string? lang = null)
    {
        var language = Languages.Normalize(lang);

        var projects = PortfolioContent.Projects
            .OrderByDescending(s => s.Featured)
            .Select(s => new Project
            {
                Title = s.Title,
                Description = s.Description.Get(language),
                TechStack = [.. s.TechStack],
                GitHubUrl = s.GitHubUrl,
                LiveDemoUrl = s.LiveDemoUrl,
                ImageUrl = s.ImageUrl,
                Featured = s.Featured
            });

        return Ok(projects);
    }
}
