using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Owner
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = new();

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}
