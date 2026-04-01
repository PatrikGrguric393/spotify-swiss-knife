using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Track
{
	[JsonPropertyName("artists")]
	private List<Artist> Artists { get; set; } = [];

	[JsonPropertyName("disc_number")]
	private int DiscNumber { get; set; }

	[JsonPropertyName("duration_ms")]
	private int DurationMs { get; set; }

	[JsonPropertyName("external_urls")]
	private ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("id")]
	private string Id { get; set; } = string.Empty;

	[JsonPropertyName("images")]
	private List<Image> Images { get; set; } = [];

	[JsonPropertyName("name")]
	private string Name { get; set; } = string.Empty;

	[JsonPropertyName("track_number")]
	private int TrackNumber { get; set; }

	[JsonPropertyName("is_local")]
	private bool IsLocal { get; set; }
}