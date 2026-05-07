using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Track
{
	[Key]
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("artists")]
	public virtual ICollection<Artist> Artists { get; set; } = new HashSet<Artist>();

	[JsonPropertyName("disc_number")]
	public int DiscNumber { get; set; }

	[JsonPropertyName("duration_ms")]
	public int DurationMs { get; set; }

	[JsonPropertyName("external_urls")]
	public ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("images")]
	public virtual ICollection<Image> Images { get; set; } = new HashSet<Image>();

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("track_number")]
	public int TrackNumber { get; set; }

	[JsonPropertyName("is_local")]
	public bool IsLocal { get; set; }

	[JsonIgnore]
	public string? AlbumId { get; set; }

	[JsonIgnore]
	[ForeignKey(nameof(AlbumId))]
	public virtual Album? Album { get; set; }
}