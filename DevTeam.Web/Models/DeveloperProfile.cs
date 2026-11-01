namespace DevTeam.Web.Models;

public class DeveloperProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public List<string> FrontendSkills { get; set; } = [];
    public List<string> BackendSkills { get; set; } = [];
    public List<string> CloudSkills { get; set; } = [];
    public List<string> Methodologies { get; set; } = [];
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string GitHubUrl { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
}
