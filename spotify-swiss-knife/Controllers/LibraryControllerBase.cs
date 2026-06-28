using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Infrastructure;

namespace spotify_swiss_knife.Controllers;

// Base class for the four server-rendered library CRUD controllers (Albums, Artists, Tracks,
// Playlists). Pins the shared route prefix, authorization policy, and Spotify-user guard so
// they can't be accidentally omitted from a new library controller.
[Route("lib")]
[Authorize(Roles = "Admin,Editor")]
[DenySpotifyUsers]
public abstract class LibraryControllerBase : Controller
{
    protected void ValidateSpotifyUrl(string? url)
    {
        if (!SpotifyUrl.IsValid(url))
            ModelState.AddModelError("SpotifyUrl", SpotifyUrl.ValidationMessage);
    }

    protected static List<SelectListItem> ToSelectList<T>(
        IEnumerable<T> items,
        Func<T, string> valueSelector,
        Func<T, string> textSelector,
        IEnumerable<string>? selectedIds = null)
    {
        var selected = new HashSet<string>(selectedIds ?? []);
        return items
            .OrderBy(textSelector, StringComparer.CurrentCultureIgnoreCase)
            .Select(i => new SelectListItem
            {
                Value = valueSelector(i),
                Text = textSelector(i),
                Selected = selected.Contains(valueSelector(i))
            })
            .ToList();
    }

    protected static List<T> FilterByIds<T>(
        IEnumerable<T> source,
        Func<T, string> idSelector,
        IEnumerable<string>? ids)
    {
        var wanted = new HashSet<string>(ids ?? []);
        if (wanted.Count == 0) return [];
        return source.Where(item => wanted.Contains(idSelector(item))).ToList();
    }
}
