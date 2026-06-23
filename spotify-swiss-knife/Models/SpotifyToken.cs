namespace spotify_swiss_knife.Models;

/// <summary>
/// Persisted Spotify OAuth credentials for one Spotify account. The access token is refreshed
/// using the refresh token once <see cref="ExpiresAt"/> passes (see SpotifyAuthService).
/// </summary>
public class SpotifyToken
{
    public int Id { get; set; }

    // Spotify user ID (e.g. "31xyzabc..."), not the ASP.NET Identity user ID.
    public string SpotifyUserId { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
