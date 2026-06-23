using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// The Spotify user that owns a <see cref="Playlist"/>, as embedded in the playlist payload.
/// Persisted as an owned value on the playlist rather than as its own table.
/// </summary>
public class Owner
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}
