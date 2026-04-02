using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Artist
{
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}