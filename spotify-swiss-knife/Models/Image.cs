using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// A single cover/profile image as returned by the Spotify Web API and persisted with the
/// owning <see cref="Album"/>, <see cref="Track"/> or <see cref="Playlist"/>.
/// </summary>
/// <remarks>
/// This is the EF-mapped image entity. <see cref="SpotifyImage"/> is a separate, unmapped
/// shape used only when deserializing the user-profile endpoint, which is never persisted.
/// </remarks>
public class Image
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }
}
