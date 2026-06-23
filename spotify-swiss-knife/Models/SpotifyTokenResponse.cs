using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models;

/// <summary>
/// Raw response from Spotify's OAuth token endpoint (/api/token), covering both the success
/// shape (access/refresh tokens) and the error shape. Transient — never persisted; the useful
/// values are copied into a <see cref="SpotifyToken"/>.
/// </summary>
public class SpotifyTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    // Lifetime of the access token in seconds from issue (Spotify currently returns 3600).
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
