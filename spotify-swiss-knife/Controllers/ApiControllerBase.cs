using Microsoft.AspNetCore.Mvc;

namespace spotify_swiss_knife.Controllers;

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
