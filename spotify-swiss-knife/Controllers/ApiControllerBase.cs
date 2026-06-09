using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace spotify_swiss_knife.Controllers;

// The CRUD API authenticates exclusively via JWT bearer tokens. Pinning the scheme here
// means [Authorize(Roles = ...)] on the actions evaluates the bearer identity rather than
// the Identity (SSKAuth) cookie used by the MVC app; [AllowAnonymous] GETs still bypass it.
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class ApiControllerBase : ControllerBase
{
    protected static bool TryValidateSpotifyUrl(string? url, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrEmpty(url)) return true;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || !parsed.Host.Contains("spotify.com"))
        {
            error = "Spotify URL must be a valid spotify.com link.";
            return false;
        }

        return true;
    }
}
