using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Track
{
	[Key]
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("artists")]
	public List<Artist> Artists { get; set; } = [];

	[JsonPropertyName("disc_number")]
	public int DiscNumber { get; set; }

	[JsonPropertyName("duration_ms")]
	public int DurationMs { get; set; }

	[JsonPropertyName("external_urls")]
	public ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("images")]
	public List<Image> Images { get; set; } = [];

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("track_number")]
	public int TrackNumber { get; set; }

	[JsonPropertyName("is_local")]
	public bool IsLocal { get; set; }

	[JsonIgnore]
	public string? AlbumId { get; set; }

	[JsonIgnore]
	public Album? Album { get; set; }
}