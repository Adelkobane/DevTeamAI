using Microsoft.AspNetCore.Components;

namespace DevTeam.Web;

public static class NavigationExtensions
{
    /// <summary>
    /// Bygger en ankarlänk som är relativ till den aktuella sidan (inklusive query-strängen).
    /// <c>&lt;base href="/"&gt;</c> gör annars att <c>href="#id"</c> blir <c>/#id</c>, vilket tar bort t.ex.
    /// <c>?utm_source=...</c> och ger en full navigering (utan scroll vid första klicket) i stället för en ren hash-växling.
    /// </summary>
    public static string AnchorHref(this NavigationManager navigation, string? id = null) =>
        new Uri(navigation.Uri).GetLeftPart(UriPartial.Query) + "#" + id;
}
