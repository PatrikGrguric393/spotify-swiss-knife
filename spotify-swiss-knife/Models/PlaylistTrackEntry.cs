using System.ComponentModel.DataAnnotations;
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
	public Playlist Playlist { get; set; } = new();

	[JsonIgnore]
	public Track Track { get; set; } = new();
}