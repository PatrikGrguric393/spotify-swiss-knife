using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// A playlist. Doubles as a Spotify deserialization target and an EF Core entity
/// (see <see cref="Album"/> for the shared pattern).
/// </summary>
public class Playlist
{
    // Backs both Items and Tracks below — see the note on those properties.
    private PlaylistTracksPage _tracks = new();

    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("images")]
    public virtual ICollection<Image> Images { get; set; } = new HashSet<Image>();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public Owner Owner { get; set; } = new Owner();

    [JsonPropertyName("snapshot_id")]
    public string SnapshotId { get; set; } = string.Empty;

    // The persisted, ordered track membership (the EF navigation). The transient paging
    // objects below carry tracks straight off a Spotify response and are not persisted.
    [JsonIgnore]
    public virtual ICollection<PlaylistTrackEntry> TrackEntries { get; set; } = new HashSet<PlaylistTrackEntry>();

    // Items and Tracks are deliberate aliases over the same backing page: Spotify's
    // "get playlist" response nests tracks under "tracks", while the "get playlist items"
    // response returns the page directly under "items". Exposing both names lets a single
    // Playlist deserialize from either shape. Neither is persisted.
    [NotMapped]
    [JsonPropertyName("items")]
    public PlaylistTracksPage Items
    {
        get => _tracks;
        set => _tracks = value ?? new PlaylistTracksPage();
    }

    [NotMapped]
    [JsonPropertyName("tracks")]
    public PlaylistTracksPage Tracks
    {
        get => _tracks;
        set => _tracks = value ?? new PlaylistTracksPage();
    }

    // UTC timestamp of the last shuffle run; null until the playlist has been shuffled once.
    public DateTime? LastShuffled { get; set; } = null;
}
