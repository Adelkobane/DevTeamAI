namespace DevTeam.Web.Models;

/// <summary>Svar från <c>GET /api/status</c>.</summary>
public class SystemStatus
{
    public bool Api { get; set; }
    public bool Azure { get; set; }
    public bool Ai { get; set; }
}
