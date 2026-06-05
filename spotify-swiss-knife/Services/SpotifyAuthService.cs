using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class SpotifyAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private readonly HttpClient _httpClient;

    private const string AuthorizeUrl = "https://accounts.spotify.com/authorize";
    private const string TokenUrl = "https://accounts.spotify.com/api/token";
    private const string ProfileUrl = "https://api.spotify.com/v1/me";
    private const string Scopes = "user-read-private user-read-email";

    public SpotifyAuthService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _clientId = config["Spotify:ClientId"]
            ?? throw new InvalidOperationException("Spotify:ClientId is not configured.");
        _clientSecret = config["Spotify:ClientSecret"]
            ?? throw new InvalidOperationException("Spotify:ClientSecret is not configured.");
        _redirectUri = config["Spotify:RedirectUri"]
            ?? throw new InvalidOperationException("Spotify:RedirectUri is not configured.");
        _httpClient = httpClientFactory.CreateClient("spotify");
    }

    public string GenerateState()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public string GetAuthorizationUrl(string state)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _redirectUri,
            ["state"] = state,
            ["scope"] = Scopes,
        };
        var queryString = string.Join("&", query.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{AuthorizeUrl}?{queryString}";
    }

    public async Task<SpotifyTokenResponse?> ExchangeCodeAsync(string code)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _redirectUri,
        });

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpotifyTokenResponse>(json);
    }

    public async Task<SpotifyTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpotifyTokenResponse>(json);
    }

    public async Task<SpotifyUserProfile?> GetUserProfileAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ProfileUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpotifyUserProfile>(json);
    }
}
