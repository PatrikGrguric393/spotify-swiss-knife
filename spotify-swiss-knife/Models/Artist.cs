using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

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

    [JsonIgnore]
    public DateTime? DeletedAt { get; set; }
}