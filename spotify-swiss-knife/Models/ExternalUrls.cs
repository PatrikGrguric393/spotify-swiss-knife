namespace spotify_swiss_knife.Models;

/// <summary>
/// Spotify's "external_urls" object. Only the public Spotify web link is kept; it is the
/// value surfaced as <c>SpotifyUrl</c> across the read DTOs.
/// </summary>
public class ExternalUrls
{
    public string Spotify { get; set; } = string.Empty;
}
