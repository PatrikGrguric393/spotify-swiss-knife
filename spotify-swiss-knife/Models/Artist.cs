using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// An artist. Doubles as a Spotify deserialization target and an EF Core entity
/// (see <see cref="Album"/> for the shared pattern).
/// </summary>
public class Artist
{
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual ICollection<Album> Albums { get; set; } = new HashSet<Album>();

    [JsonIgnore]
    public virtual ICollection<Track> Tracks { get; set; } = new HashSet<Track>();

    // Soft-delete marker: when set, the artist is treated as removed but the row is kept so
    // existing tracks/albums that reference it stay intact. Null means the artist is active.
    [JsonIgnore]
    public DateTime? DeletedAt { get; set; }
}
