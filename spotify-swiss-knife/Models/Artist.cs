using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Artist
{
    private ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("id")]
    private string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    private string Name { get; set; } = string.Empty;
}