using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

// Owns everything that talks to Spotify on a user's behalf: the OAuth handshake
// (authorization URL, code exchange, token refresh), persistence of the resulting token
// set, and the catalog/playlist calls the app builds on (profile, playlists, album
// search, bulk-save, and playlist shuffling). Tokens are stored per Spotify user so the
// background scheduler can act without an interactive session.
public class SpotifyAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private readonly HttpClient _httpClient;
    private readonly SpotifyDbContext _db;
    private readonly ILogger<SpotifyAuthService> _logger;

    private const string AuthorizeUrl = "https://accounts.spotify.com/authorize";
    private const string TokenUrl = "https://accounts.spotify.com/api/token";
    private const string ProfileUrl = "https://api.spotify.com/v1/me";
    private const string PlaylistsUrl = "https://api.spotify.com/v1/me/playlists?limit=50";
    private const string PlaylistBaseUrl = "https://api.spotify.com/v1/playlists";
    private const string SearchUrl = "https://api.spotify.com/v1/search";
    private const string AlbumBaseUrl = "https://api.spotify.com/v1/albums";
    private const string Scopes = "user-read-private user-read-email playlist-read-private " +
        "playlist-read-collaborative playlist-modify-public playlist-modify-private";

    // Max items the playlist add endpoint accepts in one POST.
    private const int AddBatchLimit = 100;

    // Max items a single replace (PUT with a uris body) can set in one request.
    private const int ReplaceBatchLimit = 100;

    public SpotifyAuthService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        SpotifyDbContext db,
        ILogger<SpotifyAuthService> logger)
    {
        _clientId = Required(config, "Spotify:ClientId");
        _clientSecret = Required(config, "Spotify:ClientSecret");
        _redirectUri = Required(config, "Spotify:RedirectUri");
        _httpClient = httpClientFactory.CreateClient("spotify");
        _db = db;
        _logger = logger;

        static string Required(IConfiguration config, string key)
        {
            var value = config[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{key} is not configured.");
            return value;
        }
    }

    // Logs a failed Spotify API response at Warning with the status code and a truncated body.
    // The access token travels in the request Authorization header, never the response body, so
    // an error body (Spotify returns a small {"error":{status,message}} document) is safe to log
    // and is the single most useful detail for telling a 401 from a 403, 429, or 5xx.
    private async Task LogApiFailureAsync(HttpResponseMessage response, string operation)
    {
        var body = await SafeReadBodyAsync(response.Content);
        _logger.LogWarning(
            "Spotify API call failed: {Operation} returned {StatusCode} {Reason}. Body: {Body}",
            operation, (int)response.StatusCode, response.ReasonPhrase, body);
    }

    private static async Task<string> SafeReadBodyAsync(HttpContent content)
    {
        try
        {
            var body = await content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return "(empty)";
            return body.Length <= 500 ? body : body[..500] + "…";
        }
        catch
        {
            return "(unreadable)";
        }
    }

    // Builds a request to the Web API carrying the user's access token as a Bearer header.
    // Every authenticated call goes through here so the auth scheme lives in one place.
    private static HttpRequestMessage BearerRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    // Serializes a payload as a JSON request body. Used for the playlist add/replace/reorder
    // calls, which all send application/json.
    private static StringContent JsonBody<T>(T payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    // Sends a request, retrying on 429 (rate limit). Spotify normally returns a Retry-After
    // header (seconds) saying exactly how long to wait, so we honor it; when it's absent we
    // fall back to exponential backoff (1s, 2s, 4s…) since a flat short wait can be far too
    // brief under heavy throttling. Takes a factory because a sent HttpRequestMessage can't be
    // reused; caps attempts so a persistently limited call still returns rather than looping.
    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> makeRequest)
    {
        const int maxAttempts = 5;
        var response = await _httpClient.SendAsync(makeRequest());
        for (var attempt = 1; attempt < maxAttempts &&
            response.StatusCode == HttpStatusCode.TooManyRequests; attempt++)
        {
            var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1 << (attempt - 1));
            await Task.Delay(wait);
            response = await _httpClient.SendAsync(makeRequest());
        }

        return response;
    }

    // Posts to the token endpoint with the client credentials in a Basic header — shared by the
    // authorization-code exchange and the refresh flow, which differ only in their form fields.
    // `operation` names the flow in the warning logged when Spotify returns no usable token.
    private async Task<SpotifyTokenResponse?> RequestTokenAsync(
        Dictionary<string, string> form, string operation)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")));
        request.Content = new FormUrlEncodedContent(form);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<SpotifyTokenResponse>(json);

        // A success body carries the tokens, so only the error fields are worth logging.
        if (tokens?.AccessToken is null || tokens.Error is not null)
        {
            _logger.LogWarning(
                "Spotify {Operation} failed: {StatusCode} {Reason}, error {Error} ({ErrorDescription}).",
                operation, (int)response.StatusCode, response.ReasonPhrase,
                tokens?.Error ?? "none", tokens?.ErrorDescription ?? "none");
        }

        return tokens;
    }

    // Writes or updates the stored token set for a Spotify user. Called after every
    // successful OAuth exchange or refresh so background jobs can authenticate.
    public async Task PersistTokensAsync(string spotifyUserId, string accessToken, string refreshToken, int expiresIn)
    {
        var record = await _db.SpotifyTokens
            .FirstOrDefaultAsync(t => t.SpotifyUserId == spotifyUserId);

        if (record is null)
        {
            record = new SpotifyToken { SpotifyUserId = spotifyUserId };
            _db.SpotifyTokens.Add(record);
        }

        record.AccessToken = accessToken;
        // Spotify only returns a new refresh token occasionally; keep the old one when absent.
        if (!string.IsNullOrEmpty(refreshToken))
            record.RefreshToken = refreshToken;
        record.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60); // 60-second safety buffer
        await _db.SaveChangesAsync();
    }

    // Returns a valid access token for the given Spotify user, refreshing if necessary.
    // Returns null if no token is stored or the refresh fails.
    public async Task<string?> GetValidAccessTokenAsync(string spotifyUserId)
    {
        var record = await _db.SpotifyTokens
            .FirstOrDefaultAsync(t => t.SpotifyUserId == spotifyUserId);

        if (record is null || string.IsNullOrEmpty(record.RefreshToken))
        {
            _logger.LogDebug("No stored Spotify refresh token for user {SpotifyUserId}.", spotifyUserId);
            return null;
        }

        if (record.ExpiresAt > DateTimeOffset.UtcNow)
            return record.AccessToken;

        var refreshed = await RefreshTokenAsync(record.RefreshToken);
        if (refreshed?.AccessToken is null || refreshed.Error is not null)
        {
            _logger.LogWarning("Spotify token refresh failed for user {SpotifyUserId}.", spotifyUserId);
            return null;
        }

        await PersistTokensAsync(spotifyUserId, refreshed.AccessToken,
            refreshed.RefreshToken ?? record.RefreshToken, refreshed.ExpiresIn);

        return refreshed.AccessToken;
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

    // Exchanges an authorization code (from the OAuth redirect) for an initial token set.
    public Task<SpotifyTokenResponse?> ExchangeCodeAsync(string code) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _redirectUri,
        }, "authorization-code exchange");

    // Trades a stored refresh token for a fresh access token (and occasionally a new refresh token).
    public Task<SpotifyTokenResponse?> RefreshTokenAsync(string refreshToken) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        }, "token refresh");

    public async Task<SpotifyUserProfile?> GetUserProfileAsync(string accessToken)
    {
        var request = BearerRequest(HttpMethod.Get, ProfileUrl, accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            await LogApiFailureAsync(response, "GET /me (user profile)");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpotifyUserProfile>(json);
    }

    // Returns the playlists owned or followed by the current user, or null if the
    // request fails (expired token, missing scope, rate limit, etc.). The list
    // endpoint returns simplified playlists whose tracks field is only a {href, total}
    // reference, so track items are not populated here.
    public async Task<List<Playlist>?> GetUserPlaylistsAsync(string accessToken)
    {
        var playlists = new List<Playlist>();
        var url = PlaylistsUrl;

        // Follow paging links, capped so a malformed response can't loop forever.
        for (var page = 0; page < 40 && url is not null; page++)
        {
            var request = BearerRequest(HttpMethod.Get, url, accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await LogApiFailureAsync(response, $"GET /me/playlists (page {page})");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var pageResult = JsonSerializer.Deserialize<PlaylistsPage>(json);
            if (pageResult is null)
            {
                _logger.LogWarning("Spotify playlists response on page {Page} could not be parsed.", page);
                return null;
            }

            // The schema allows null entries when a playlist is no longer available.
            playlists.AddRange(pageResult.Items.Where(playlist => playlist is not null));
            url = pageResult.Next;
        }

        return playlists;
    }

    // Searches the public Spotify catalog for albums matching the query. Needs only a
    // valid token (no extra scope). Returns simplified albums, or null if the request
    // fails (expired token, rate limit, etc.). `limit` is capped at the API maximum of 10.
    public async Task<List<Album>?> SearchAlbumsAsync(string accessToken, string query, int limit = 10)
    {
        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return [];

        var url = $"{SearchUrl}?q={Uri.EscapeDataString(trimmed)}&type=album" +
            $"&limit={Math.Clamp(limit, 1, 10)}";
        var request = BearerRequest(HttpMethod.Get, url, accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            await LogApiFailureAsync(response, "GET /search?type=album");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AlbumSearchResponse>(json);
        // The schema allows null entries in search results when an item is unavailable.
        return result?.Albums?.Items.Where(album => album is not null).ToList() ?? [];
    }

    // Returns the track URIs for an album in track order, following paging until the album
    // is exhausted. Returns null if any page fails. Local files can't appear on catalog
    // albums, so every track exposes a stable URI suitable for adding to a playlist.
    public async Task<List<string>?> GetAlbumTrackUrisAsync(string accessToken, string albumId)
    {
        var uris = new List<string>();
        var url = $"{AlbumBaseUrl}/{Uri.EscapeDataString(albumId)}/tracks?limit=50";

        for (var page = 0; page < 40 && url is not null; page++)
        {
            var request = BearerRequest(HttpMethod.Get, url, accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await LogApiFailureAsync(response, $"GET /albums/{albumId}/tracks (page {page})");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var pageResult = JsonSerializer.Deserialize<AlbumTrackUriPage>(json);
            if (pageResult is null)
            {
                _logger.LogWarning(
                    "Spotify album-tracks response for {AlbumId} on page {Page} could not be parsed.", albumId, page);
                return null;
            }

            uris.AddRange(pageResult.Items
                .Select(item => item.Uri)
                .Where(uri => !string.IsNullOrEmpty(uri))!);
            url = pageResult.Next;
        }

        return uris;
    }

    // Appends track URIs to a playlist in batches of 100 (the API per-request maximum),
    // using the documented add endpoint POST /playlists/{id}/items. Requires a
    // playlist-modify-* scope. Returns false if any batch is rejected.
    public async Task<bool> AddTracksToPlaylistAsync(
        string accessToken, string playlistId, IReadOnlyList<string> uris)
    {
        var url = $"{PlaylistBaseUrl}/{Uri.EscapeDataString(playlistId)}/items";

        for (var offset = 0; offset < uris.Count; offset += AddBatchLimit)
        {
            var batch = uris.Skip(offset).Take(AddBatchLimit).ToList();
            var response = await SendWithRetryAsync(() =>
            {
                var request = BearerRequest(HttpMethod.Post, url, accessToken);
                request.Content = JsonBody(new AddTracksRequest { Uris = batch });
                return request;
            });
            if (!response.IsSuccessStatusCode)
            {
                await LogApiFailureAsync(response,
                    $"POST /playlists/{playlistId}/items (batch at offset {offset})");
                return false;
            }
        }

        return true;
    }

    // Collects every track from the selected albums and appends them to each selected
    // playlist. An album that can't be read or a playlist that can't be written is recorded
    // as a warning and skipped rather than aborting the whole operation, so a single bad id
    // doesn't lose the rest of the work.
    public async Task<BulkAlbumSaveResult> BulkSaveAlbumsToPlaylistsAsync(
        string accessToken, IReadOnlyList<string> albumIds, IReadOnlyList<string> playlistIds)
    {
        if (albumIds.Count == 0)
            return BulkAlbumSaveResult.Fail("Select at least one album.");
        if (playlistIds.Count == 0)
            return BulkAlbumSaveResult.Fail("Select at least one playlist.");

        var warnings = new List<string>();
        var uris = new List<string>();
        var albumsAdded = 0;

        foreach (var albumId in albumIds.Distinct())
        {
            var albumUris = await GetAlbumTrackUrisAsync(accessToken, albumId);
            if (albumUris is null)
            {
                warnings.Add($"Couldn't read tracks for album {albumId}; it was skipped.");
                continue;
            }

            uris.AddRange(albumUris);
            albumsAdded++;
        }

        if (uris.Count == 0)
            return BulkAlbumSaveResult.Fail("None of the selected albums had any tracks to add.");

        var playlistsUpdated = 0;
        foreach (var playlistId in playlistIds.Distinct())
        {
            var ok = await AddTracksToPlaylistAsync(accessToken, playlistId, uris);
            if (!ok)
            {
                warnings.Add($"Couldn't add tracks to playlist {playlistId}; it was skipped.");
                continue;
            }

            playlistsUpdated++;
        }

        if (playlistsUpdated == 0)
        {
            return BulkAlbumSaveResult.Fail(
                "We couldn't update any of the selected playlists. Your session may have expired or " +
                "lacks permission to modify them — please disconnect and reconnect Spotify.");
        }

        return new BulkAlbumSaveResult(true, null, albumsAdded, uris.Count, playlistsUpdated, warnings);
    }

    // Shuffles a Spotify playlist in place. Requires playlist-modify-* scopes.
    //
    // Replace path (no local files): the whole playlist is rewritten in ceil(n/100)
    // requests — a PUT of the first shuffled batch overwrites it, then any remainder is
    // appended. Independent of length, this stays well under the rate limit.
    //
    // Reorder fallback (has local files): the API can't re-add local files by URI, so those
    // playlists use a non-destructive index reorder — one request per moved item, which is
    // 429-retried — that preserves local files and metadata.
    public async Task<SpotifyShuffleResult> ShufflePlaylistAsync(
        string accessToken, string playlistId)
    {
        var items = await GetAllItemsAsync(accessToken, playlistId);
        if (items is null)
        {
            return SpotifyShuffleResult.Fail(
                "We couldn't read that Spotify playlist. Your session may have expired or lacks the " +
                "permission to modify it — please disconnect and reconnect Spotify.");
        }

        if (items.Count <= 1)
        {
            return SpotifyShuffleResult.Ok(items.Count, 0);
        }

        // Replace only works when every item can be re-added by URI; local files can't, so
        // their presence forces the reorder fallback.
        var noLocalFiles = items.All(item => !item.IsLocal && !string.IsNullOrEmpty(item.Uri));
        if (noLocalFiles)
        {
            return await ReplaceShuffleAsync(accessToken, playlistId, items.Select(item => item.Uri!).ToList());
        }

        return await ReorderShuffleAsync(accessToken, playlistId, items.Count);
    }

    private async Task<SpotifyShuffleResult> ReplaceShuffleAsync(
        string accessToken, string playlistId, List<string> uris)
    {
        var shuffled = PlaylistShuffler.Shuffle(uris);
        var moved = shuffled.Where((uri, index) => uri != uris[index]).Count();

        // A replace sets at most one batch, so the first batch overwrites the playlist and any
        // remainder is appended in order.
        var ok = await ReplacePlaylistItemsAsync(accessToken, playlistId, shuffled.Take(ReplaceBatchLimit).ToList());
        if (ok && shuffled.Count > ReplaceBatchLimit)
        {
            ok = await AddTracksToPlaylistAsync(accessToken, playlistId, shuffled.Skip(ReplaceBatchLimit).ToList());
        }

        return ok
            ? SpotifyShuffleResult.Ok(shuffled.Count, moved)
            : SpotifyShuffleResult.Fail(
                "Spotify rejected the shuffle. Your session may have expired or lacks permission to " +
                "modify this playlist — please disconnect and reconnect Spotify.");
    }

    // Non-destructive reorder: realize the target order with single-item moves, chaining
    // the snapshot ID returned by each move so concurrent edits are detected.
    private async Task<SpotifyShuffleResult> ReorderShuffleAsync(
        string accessToken, string playlistId, int total)
    {
        var snapshotId = await GetPlaylistSnapshotIdAsync(accessToken, playlistId);
        if (snapshotId is null)
        {
            return SpotifyShuffleResult.Fail(
                "We couldn't read that Spotify playlist. Your session may have expired or lacks the " +
                "permission to modify it — please disconnect and reconnect Spotify.");
        }

        var target = PlaylistShuffler.Shuffle(Enumerable.Range(0, total).ToList());
        var current = Enumerable.Range(0, total).ToList();
        var moved = 0;

        for (var position = 0; position < total; position++)
        {
            // Positions before `position` already hold their target item, so the item we
            // want here can only be at `position` or later.
            var from = current.IndexOf(target[position]);
            if (from == position)
            {
                continue;
            }

            var newSnapshot = await ReorderPlaylistItemAsync(accessToken, playlistId, from, position, snapshotId);
            if (newSnapshot is null)
            {
                return SpotifyShuffleResult.Partial(total, moved,
                    $"Spotify stopped accepting changes after {moved} move(s). The playlist is intact but " +
                    "only partially shuffled — please try again.");
            }

            snapshotId = newSnapshot;
            current.RemoveAt(from);
            current.Insert(position, target[position]);
            moved++;
        }

        return SpotifyShuffleResult.Ok(total, moved);
    }

    // Reads every item in the playlist (URIs + local flags), following pagination. Both the
    // strategy choice (any local files?) and the replace path's URI list need the full set.
    // Returns null if any page read fails.
    private async Task<List<ShuffleItem>?> GetAllItemsAsync(string accessToken, string playlistId)
    {
        var items = new List<ShuffleItem>();
        string? url = $"{PlaylistBaseUrl}/{Uri.EscapeDataString(playlistId)}/items" +
            $"?limit={ReplaceBatchLimit}&fields=next,items(is_local,item(uri),track(uri))";

        while (url is not null)
        {
            var request = BearerRequest(HttpMethod.Get, url, accessToken);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await LogApiFailureAsync(response, $"GET /playlists/{playlistId}/items (shuffle read)");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var page = JsonSerializer.Deserialize<ShuffleItemsPage>(json);
            if (page is null)
            {
                return null;
            }

            items.AddRange(page.Items);
            url = page.Next;
        }

        return items;
    }

    private async Task<bool> ReplacePlaylistItemsAsync(string accessToken, string playlistId, List<string> uris)
    {
        var url = $"{PlaylistBaseUrl}/{Uri.EscapeDataString(playlistId)}/items";
        var response = await SendWithRetryAsync(() =>
        {
            var request = BearerRequest(HttpMethod.Put, url, accessToken);
            request.Content = JsonBody(new PlaylistReplaceRequest { Uris = uris });
            return request;
        });

        if (!response.IsSuccessStatusCode)
        {
            await LogApiFailureAsync(response, $"PUT /playlists/{playlistId}/items (shuffle replace)");
            return false;
        }

        return true;
    }

    // Reads the playlist's current snapshot id, the concurrency token each reorder must chain
    // off so Spotify can reject the change if the playlist was edited in the meantime.
    private async Task<string?> GetPlaylistSnapshotIdAsync(string accessToken, string playlistId)
    {
        var url = $"{PlaylistBaseUrl}/{Uri.EscapeDataString(playlistId)}?fields=snapshot_id";
        var request = BearerRequest(HttpMethod.Get, url, accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            await LogApiFailureAsync(response, $"GET /playlists/{playlistId} (shuffle state)");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var snapshot = JsonSerializer.Deserialize<PlaylistSnapshot>(json);
        if (snapshot is null || string.IsNullOrEmpty(snapshot.SnapshotId))
        {
            _logger.LogWarning(
                "Spotify playlist {PlaylistId} returned no snapshot id; cannot reorder for shuffle.", playlistId);
            return null;
        }

        return snapshot.SnapshotId;
    }

    private async Task<string?> ReorderPlaylistItemAsync(
        string accessToken, string playlistId, int rangeStart, int insertBefore, string snapshotId)
    {
        var url = $"{PlaylistBaseUrl}/{Uri.EscapeDataString(playlistId)}/items";
        var response = await SendWithRetryAsync(() =>
        {
            var request = BearerRequest(HttpMethod.Put, url, accessToken);
            request.Content = JsonBody(new PlaylistReorderRequest
            {
                RangeStart = rangeStart,
                InsertBefore = insertBefore,
                RangeLength = 1,
                SnapshotId = snapshotId,
            });
            return request;
        });

        if (!response.IsSuccessStatusCode)
        {
            await LogApiFailureAsync(response,
                $"PUT /playlists/{playlistId}/items (reorder {rangeStart}->{insertBefore})");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PlaylistSnapshot>(json);
        return string.IsNullOrEmpty(result?.SnapshotId) ? snapshotId : result.SnapshotId;
    }

    private sealed class PlaylistsPage
    {
        [JsonPropertyName("items")]
        public List<Playlist> Items { get; set; } = [];

        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }

    private sealed class AlbumSearchResponse
    {
        [JsonPropertyName("albums")]
        public SimplifiedAlbumsPage? Albums { get; set; }
    }

    private sealed class SimplifiedAlbumsPage
    {
        [JsonPropertyName("items")]
        public List<Album> Items { get; set; } = [];

        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }

    private sealed class AlbumTrackUriPage
    {
        [JsonPropertyName("items")]
        public List<UriRef> Items { get; set; } = [];

        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }

    private sealed class AddTracksRequest
    {
        [JsonPropertyName("uris")]
        public List<string> Uris { get; set; } = [];
    }

    private sealed class ShuffleItemsPage
    {
        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("items")]
        public List<ShuffleItem> Items { get; set; } = [];
    }

    private sealed class ShuffleItem
    {
        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }

        // `item` is the current field; `track` is deprecated but still returned, so we
        // read whichever is present.
        [JsonPropertyName("item")]
        public UriRef? Item { get; set; }

        [JsonPropertyName("track")]
        public UriRef? Track { get; set; }

        public string? Uri => Item?.Uri ?? Track?.Uri;
    }

    private sealed class UriRef
    {
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
    }

    private sealed class PlaylistReplaceRequest
    {
        [JsonPropertyName("uris")]
        public List<string> Uris { get; set; } = [];
    }

    private sealed class PlaylistReorderRequest
    {
        [JsonPropertyName("range_start")]
        public int RangeStart { get; set; }

        [JsonPropertyName("insert_before")]
        public int InsertBefore { get; set; }

        [JsonPropertyName("range_length")]
        public int RangeLength { get; set; }

        [JsonPropertyName("snapshot_id")]
        public string SnapshotId { get; set; } = string.Empty;
    }

    private sealed class PlaylistSnapshot
    {
        [JsonPropertyName("snapshot_id")]
        public string SnapshotId { get; set; } = string.Empty;
    }
}
