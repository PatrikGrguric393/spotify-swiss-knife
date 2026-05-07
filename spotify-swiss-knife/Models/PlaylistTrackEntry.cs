using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class PlaylistTrackEntry
{
	[Key]
	public int Id { get; set; }

	public string PlaylistId { get; set; } = string.Empty;

	public string TrackId { get; set; } = string.Empty;

	public int SortOrder { get; set; }

	[JsonIgnore]
	[ForeignKey(nameof(PlaylistId))]
	public virtual Playlist Playlist { get; set; } = null!;

	[JsonIgnore]
	[ForeignKey(nameof(TrackId))]
	public virtual Track Track { get; set; } = null!;
}