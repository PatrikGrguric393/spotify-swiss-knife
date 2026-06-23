using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// Spotify's generic paging object (href/limit/next/offset/previous/total/items). Concrete
/// pages derive from this so each endpoint's item type is strongly typed.
/// </summary>
public abstract class SpotifyPage<T>
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("previous")]
    public string? Previous { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = [];
}

/// <summary>A page of an album's tracks; items are bare <see cref="Track"/> objects.</summary>
public class AlbumTracksPage : SpotifyPage<Track> { }

/// <summary>
/// A page of a playlist's tracks. Spotify wraps each entry in an object whose "track" field
/// holds the actual track, modelled by <see cref="PlaylistTrack"/>.
/// </summary>
public class PlaylistTracksPage : SpotifyPage<PlaylistTrack> { }

/// <summary>The per-item wrapper Spotify uses inside a playlist's track page.</summary>
public class PlaylistTrack
{
    [JsonPropertyName("track")]
    public Track Track { get; set; } = new();
}
