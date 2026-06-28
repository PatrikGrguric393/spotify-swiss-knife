using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// Spotify's "external_urls" object. Only the public Spotify web link is kept; it is the
/// value surfaced as <c>SpotifyUrl</c> across the read DTOs.
/// </summary>
public class ExternalUrls
{
    [JsonPropertyName("spotify")]
    public string Spotify { get; set; } = string.Empty;

    // Spotify returns an empty string rather than null when no URL exists.
    // Use this instead of .Spotify directly to get a clean nullable value.
    [JsonIgnore]
    public string? SpotifyUrl => string.IsNullOrEmpty(Spotify) ? null : Spotify;
}
