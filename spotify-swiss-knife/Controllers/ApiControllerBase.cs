using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Infrastructure;

namespace spotify_swiss_knife.Controllers;

// Shared base for the JSON CRUD API controllers (api/albums, api/artists, api/tracks,
// api/playlists). The API authenticates exclusively via JWT bearer tokens. Pinning the scheme
// here means [Authorize(Roles = ...)] on the actions evaluates the bearer identity rather than
// the Identity cookie used by the MVC app; [AllowAnonymous] GETs still bypass it.
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class ApiControllerBase : ControllerBase
{
    // Validates the optional Spotify URL on a create/update DTO. Returns false with a
    // user-facing message when the value is present but isn't a valid spotify.com link.
    protected static bool TryValidateSpotifyUrl(string? url, out string error)
    {
        if (SpotifyUrl.IsValid(url))
        {
            error = string.Empty;
            return true;
        }

        error = SpotifyUrl.ValidationMessage;
        return false;
    }

    // Applies the shared list-search behaviour used by every CRUD GetAll endpoint: a blank query
    // returns the source unchanged, otherwise items are kept when the query exactly matches the id
    // or appears anywhere in the name (both case-insensitive). Selectors let each controller point
    // at its own entity's id/name without duplicating the filter.
    protected static IEnumerable<T> ApplySearchFilter<T>(
        IEnumerable<T> items, string? query, Func<T, string> idSelector, Func<T, string> nameSelector)
    {
        if (string.IsNullOrWhiteSpace(query))
            return items;

        var term = query.Trim();
        return items
            .Where(item => idSelector(item).Equals(term, StringComparison.OrdinalIgnoreCase)
                           || nameSelector(item).Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
