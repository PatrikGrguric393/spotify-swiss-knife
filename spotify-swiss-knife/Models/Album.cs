using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Album
{
	[Key]
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("album_type")]
	public string AlbumType { get; set; } = string.Empty;

	[JsonPropertyName("total_tracks")]
	public int TotalTracks { get; set; }

	[JsonPropertyName("external_urls")]
	public ExternalUrls ExternalUrls { get; set; } = new();

	[JsonPropertyName("images")]
	public virtual ICollection<Image> Images { get; set; } = new HashSet<Image>();

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("release_date")]
	public string ReleaseDate { get; set; } = string.Empty;

	[JsonPropertyName("release_date_precision")]
	public string ReleaseDatePrecision { get; set; } = string.Empty;

	[JsonPropertyName("artists")]
	public virtual ICollection<Artist> Artists { get; set; } = new HashSet<Artist>();

	[JsonIgnore]
	public virtual ICollection<Track> TrackList { get; set; } = new HashSet<Track>();

	[NotMapped]
	[JsonPropertyName("tracks")]
	public AlbumTracksPage Tracks { get; set; } = new();

	[JsonPropertyName("label")]
	public string? Label { get; set; }

	[JsonPropertyName("popularity")]
	public int Popularity { get; set; }

	[JsonIgnore]
	public string? CoverImageFileName { get; set; }

	[JsonIgnore]
	public string? CoverImageContentType { get; set; }

	[NotMapped]
	[JsonIgnore]
	public bool HasCover => !string.IsNullOrEmpty(CoverImageFileName);
}