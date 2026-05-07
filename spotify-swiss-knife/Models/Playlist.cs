using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Playlist
{
	private PlaylistTracksPage _tracks = new();

	[Key]
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("description")]
	public string Description { get; set; } = string.Empty;

	[JsonPropertyName("external_urls")]
	public ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("images")]
	public List<Image> Images { get; set; } = [];

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("owner")]
	public Owner Owner { get; set; } = new Owner();

	[JsonPropertyName("snapshot_id")]
	public string SnapshotId { get; set; } = string.Empty;

    

	[JsonIgnore]
	public ICollection<PlaylistTrackEntry> TrackEntries { get; set; } = [];

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

	public DateTime? LastShuffled { get; set; } = null;
}