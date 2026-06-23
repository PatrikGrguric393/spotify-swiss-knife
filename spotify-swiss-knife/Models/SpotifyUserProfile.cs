using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// The current user's Spotify profile (/me). Transient deserialization target; not persisted.
/// </summary>
public class SpotifyUserProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("images")]
    public List<SpotifyImage>? Images { get; set; }
}

/// <summary>
/// Profile image shape used only by <see cref="SpotifyUserProfile"/>. Kept separate from the
/// persisted <see cref="Image"/> entity so profile data stays purely transient (unmapped).
/// </summary>
public class SpotifyImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }
}
