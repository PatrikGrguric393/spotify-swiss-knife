using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// Join row linking a <see cref="Playlist"/> to a <see cref="Track"/> while preserving order.
/// Because the same track can sit at several positions, this is a first-class entity with its
/// own key rather than a plain many-to-many join.
/// </summary>
public class PlaylistTrackEntry
{
    [Key]
    public int Id { get; set; }

    public string PlaylistId { get; set; } = string.Empty;

    public string TrackId { get; set; } = string.Empty;

    // Zero-based position of the track within the playlist; defines play order.
    public int SortOrder { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(PlaylistId))]
    public virtual Playlist Playlist { get; set; } = null!;

    [JsonIgnore]
    [ForeignKey(nameof(TrackId))]
    public virtual Track Track { get; set; } = null!;
}
