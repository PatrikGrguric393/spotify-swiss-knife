using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Artist
{
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<Album> Albums { get; set; } = [];

    [JsonIgnore]
    public ICollection<Track> Tracks { get; set; } = [];
}