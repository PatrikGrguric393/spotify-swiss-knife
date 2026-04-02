using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Playlist
{
	[JsonPropertyName("description")]
	private string Description { get; set; } = string.Empty;

	[JsonPropertyName("external_urls")]
	private ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("id")]
	private string Id { get; set; } = string.Empty;

	[JsonPropertyName("images")]
	private List<Image> Images { get; set; } = [];

	[JsonPropertyName("name")]
	private string Name { get; set; } = string.Empty;

	[JsonPropertyName("owner")]
	private Owner Owner { get; set; } = new();

	[JsonPropertyName("snapshot_id")]
	private string SnapshotId { get; set; } = string.Empty;

	[JsonPropertyName("items")]
	private PlaylistTracksPage Items { get; set; } = new();

	[JsonPropertyName("tracks")]
	private PlaylistTracksPage Tracks { get; set; } = new();

	private DateTime? LastShuffled { get; set; } = null;
}

public class Owner
{
	[JsonPropertyName("external_urls")]
	private ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("display_name")]
	private string? DisplayName { get; set; } = string.Empty;
}