using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Playlist
{
	[JsonPropertyName("description")]
	public string Description { get; set; } = string.Empty;

	[JsonPropertyName("external_urls")]
	public ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("images")]
	public List<Image> Images { get; set; } = [];

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("owner")]
	public Owner Owner { get; set; } = new();

	[JsonPropertyName("snapshot_id")]
	public string SnapshotId { get; set; } = string.Empty;

	[JsonPropertyName("items")]
	public PlaylistTracksPage Items { get; set; } = new();

	[JsonPropertyName("tracks")]
	public PlaylistTracksPage Tracks { get; set; } = new();

	public DateTime? LastShuffled { get; set; } = null;
}

public class Owner
{
	[JsonPropertyName("external_urls")]
	public ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("display_name")]
	public string? DisplayName { get; set; } = string.Empty;
}