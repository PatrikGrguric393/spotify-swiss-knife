using System.Text.Json.Serialization;

namespace spotify_swiss_knife.Models.Dtos;

/// <summary>
/// The app's own OAuth-style token response: a bearer access token plus the refresh token
/// used to obtain a new one. Distinct from <see cref="spotify_swiss_knife.Models.SpotifyTokenResponse"/>,
/// which models Spotify's token endpoint.
/// </summary>
public class TokenResponseDto
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}
