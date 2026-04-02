using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class AlbumTracksPage
{
	[JsonPropertyName("href")]
	public string Href { get; set; } = string.Empty;

	[JsonPropertyName("limit")]
	public int Limit { get; set; }

	[JsonPropertyName("next")]
	public string? Next { get; set; }

	[JsonPropertyName("offset")]
	public int Offset { get; set; }

	[JsonPropertyName("previous")]
	public string? Previous { get; set; }

	[JsonPropertyName("total")]
	public int Total { get; set; }

	[JsonPropertyName("items")]
	public List<Track> Items { get; set; } = [];
}

public class PlaylistTracksPage
{
	[JsonPropertyName("href")]
	public string Href { get; set; } = string.Empty;

	[JsonPropertyName("limit")]
	public int Limit { get; set; }

	[JsonPropertyName("next")]
	public string? Next { get; set; }

	[JsonPropertyName("offset")]
	public int Offset { get; set; }

	[JsonPropertyName("previous")]
	public string? Previous { get; set; }

	[JsonPropertyName("total")]
	public int Total { get; set; }

	[JsonPropertyName("items")]
	public List<PlaylistTrack> Items { get; set; } = [];
}

public class PlaylistTrack
{
	[JsonPropertyName("track")]
	public Track Track { get; set; } = new();
}