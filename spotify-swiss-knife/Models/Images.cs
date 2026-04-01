using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

public class Image
{
    [JsonPropertyName("url")]
    private string Url { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    private int Height { get; set; }

    [JsonPropertyName("width")]
    private int Width { get; set; }
}