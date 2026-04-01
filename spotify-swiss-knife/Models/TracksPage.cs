using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class AlbumTracksPage
{
	[JsonPropertyName("href")]
	private string Href { get; set; } = string.Empty;

	[JsonPropertyName("limit")]
	private int Limit { get; set; }

	[JsonPropertyName("next")]
	private string? Next { get; set; }

	[JsonPropertyName("offset")]
	private int Offset { get; set; }

	[JsonPropertyName("previous")]
	private string? Previous { get; set; }

	[JsonPropertyName("total")]
	private int Total { get; set; }

	[JsonPropertyName("items")]
	private List<Track> Items { get; set; } = [];
}

public class PlaylistTracksPage
{
	[JsonPropertyName("href")]
	private string Href { get; set; } = string.Empty;

	[JsonPropertyName("limit")]
	private int Limit { get; set; }

	[JsonPropertyName("next")]
	private string? Next { get; set; }

	[JsonPropertyName("offset")]
	private int Offset { get; set; }

	[JsonPropertyName("previous")]
	private string? Previous { get; set; }

	[JsonPropertyName("total")]
	private int Total { get; set; }

	[JsonPropertyName("items")]
	private List<PlaylistTrack> Items { get; set; } = [];
}

public class PlaylistTrack
{
	[JsonPropertyName("track")]
	private Track Track { get; set; } = new();
}