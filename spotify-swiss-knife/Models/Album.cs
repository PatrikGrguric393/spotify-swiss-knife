using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Album
{
	[JsonPropertyName("album_type")]
	private string AlbumType { get; set; } = string.Empty;

	[JsonPropertyName("total_tracks")]
	private int TotalTracks { get; set; }

	[JsonPropertyName("external_urls")]
	private ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("id")]
	private string Id { get; set; } = string.Empty;

	[JsonPropertyName("images")]
	private List<Image> Images { get; set; } = [];

	[JsonPropertyName("name")]
	private string Name { get; set; } = string.Empty;

	[JsonPropertyName("release_date")]
	private string ReleaseDate { get; set; } = string.Empty;

	[JsonPropertyName("release_date_precision")]
	private string ReleaseDatePrecision { get; set; } = string.Empty;

	[JsonPropertyName("artists")]
	private List<Artist> Artists { get; set; } = [];

	[JsonPropertyName("tracks")]
	private AlbumTracksPage Tracks { get; set; } = new();

	[JsonPropertyName("label")]
	private string Label { get; set; } = string.Empty;

	[JsonPropertyName("popularity")]
	private int Popularity { get; set; }
}