namespace spotify_swiss_knife.Infrastructure;

// Shared validation for the optional "Spotify URL" fields that artists, albums, tracks, and
// playlists carry. Centralised here so the MVC controllers and the JWT CRUD API enforce the
// exact same rule (they previously each kept their own near-identical copy).
public static class SpotifyUrl
{
    public const string ValidationMessage = "Spotify URL must be a valid spotify.com link.";

    // The field is optional, so a null/empty value is considered valid. A non-empty value must
    // be an absolute URL whose host is spotify.com or one of its subdomains (e.g.
    // open.spotify.com). The host is matched on a "." boundary so look-alikes such as
    // "spotify.com.evil.test" or "notspotify.com" are rejected.
    public static bool IsValid(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            return false;

        return parsed.Host.Equals("spotify.com", StringComparison.OrdinalIgnoreCase)
            || parsed.Host.EndsWith(".spotify.com", StringComparison.OrdinalIgnoreCase);
    }
}
