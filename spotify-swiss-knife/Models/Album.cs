using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// An album. Like the other catalog entities (<see cref="Artist"/>, <see cref="Track"/>,
/// <see cref="Playlist"/>) this class plays a dual role: the <c>[JsonPropertyName]</c>
/// attributes let it deserialize directly from Spotify Web API responses, and the EF Core
/// annotations let the same instance be persisted to the local database.
/// </summary>
public class Album
{
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("album_type")]
    public string AlbumType { get; set; } = string.Empty;

    [JsonPropertyName("total_tracks")]
    public int TotalTracks { get; set; }

    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("images")]
    public virtual ICollection<Image> Images { get; set; } = new HashSet<Image>();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; } = string.Empty;

    [JsonPropertyName("release_date_precision")]
    public string ReleaseDatePrecision { get; set; } = string.Empty;

    [JsonPropertyName("artists")]
    public virtual ICollection<Artist> Artists { get; set; } = new HashSet<Artist>();

    // The persisted set of tracks belonging to this album (the EF navigation). Spotify sends
    // tracks under "tracks" as a paging object, so that arrives via the unmapped Tracks below.
    [JsonIgnore]
    public virtual ICollection<Track> TrackList { get; set; } = new HashSet<Track>();

    // Transient paging object populated when deserializing a Spotify response; not persisted.
    // Code that needs the canonical track set should prefer TrackList.
    [NotMapped]
    [JsonPropertyName("tracks")]
    public AlbumTracksPage Tracks { get; set; } = new();

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("popularity")]
    public int Popularity { get; set; }

    // Locally uploaded cover image, stored on disk via AlbumCoverStorage. Independent of the
    // Spotify-provided Images above; null when the user hasn't uploaded one.
    [JsonIgnore]
    public string? CoverImageFileName { get; set; }

    [JsonIgnore]
    public string? CoverImageContentType { get; set; }

    [NotMapped]
    [JsonIgnore]
    public bool HasCover => !string.IsNullOrEmpty(CoverImageFileName);
}
