using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[Route("shuffle")]
public class ShuffleController : Controller
{
    private readonly PlaylistRepository _playlistRepository;
    private readonly SpotifyAuthService _spotifyAuth;

    public ShuffleController(PlaylistRepository playlistRepository, SpotifyAuthService spotifyAuth)
    {
        _playlistRepository = playlistRepository;
        _spotifyAuth = spotifyAuth;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var source = await ResolvePlaylistsAsync();
        var viewModel = ShufflePlaylistPage.Create(source.Playlists, errorMessage: source.Error);
        return View(viewModel);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([FromForm] PlaylistShuffleForm input)
    {
        var source = await ResolvePlaylistsAsync();

        if (source.Error is not null)
            return Json(new ShuffleJsonResult(false, source.Error, null));

        var selectedPlaylist = source.Playlists.FirstOrDefault(playlist => playlist.Id == input.PlaylistId);
        if (selectedPlaylist is null)
            return Json(new ShuffleJsonResult(false, "Please select a valid playlist.", null));

        if (source.IsSpotify)
            return await ShuffleSpotifyPlaylistJsonAsync(source, selectedPlaylist, input);

        if (!ModelState.IsValid)
            return Json(new ShuffleJsonResult(false, "Invalid form data.", null));

        var statusMessage = ExecuteShuffle(selectedPlaylist, input.RandomnessLevel);
        return Json(new ShuffleJsonResult(true, statusMessage, selectedPlaylist.LastShuffled?.ToString("o")));
    }

    private async Task<IActionResult> ShuffleSpotifyPlaylistJsonAsync(
        PlaylistSource source, Playlist selectedPlaylist, PlaylistShuffleForm input)
    {
        var result = await _spotifyAuth.ShufflePlaylistAsync(
            source.AccessToken!, selectedPlaylist.Id, input.RandomnessLevel);

        if (!result.Succeeded)
            return Json(new ShuffleJsonResult(false, result.Error ?? "Shuffle failed.", null));

        var statusMessage = $"Shuffle completed for '{selectedPlaylist.Name}'. " +
            $"Tracks: {result.TrackCount}, moved: {result.MovedCount}, randomness: {input.RandomnessLevel}.";
        return Json(new ShuffleJsonResult(true, statusMessage, DateTime.UtcNow.ToString("o")));
    }

    private sealed record ShuffleJsonResult(bool Success, string Message, string? ShuffledAtUtc);

    // Picks the playlist source from the current login: Spotify if connected, otherwise
    // the local database for a signed-in app user, otherwise an error for anonymous users.
    private async Task<PlaylistSource> ResolvePlaylistsAsync()
    {
        var spotifyAuth = await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        if (spotifyAuth.Succeeded)
        {
            var accessToken = spotifyAuth.Principal?.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(accessToken))
            {
                return new PlaylistSource([], true, null,
                    "Your Spotify connection is missing an access token. Please disconnect and reconnect Spotify.");
            }

            var playlists = await _spotifyAuth.GetUserPlaylistsAsync(accessToken);
            if (playlists is null)
            {
                return new PlaylistSource([], true, null,
                    "We couldn't load your Spotify playlists. Your session may have expired or lacks the required " +
                    "permission — please disconnect and reconnect Spotify.");
            }

            return new PlaylistSource(playlists, true, accessToken, null);
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            return new PlaylistSource(_playlistRepository.GetAll(), false, null, null);
        }

        return new PlaylistSource([], false, null,
            "You're not logged in. Connect Spotify or sign in with a local account to load playlists to shuffle.");
    }

    private string ExecuteShuffle(Playlist playlist, ShuffleRandomnessLevel randomnessLevel)
    {
        var originalItems = playlist.Tracks.Items.ToList();
        var shuffledItems = PlaylistShuffler.Shuffle(originalItems, randomnessLevel);

        var originalPositions = originalItems
            .Select((item, index) => new { item.Track.Id, index })
            .ToDictionary(entry => entry.Id, entry => entry.index);

        var movedCount = shuffledItems
            .Select((item, index) => new { item.Track.Id, index })
            .Count(entry => originalPositions.TryGetValue(entry.Id, out var originalIndex) && originalIndex != entry.index);

        playlist.Tracks.Items = shuffledItems;
        playlist.LastShuffled = DateTime.UtcNow;

        return $"Shuffle completed for '{playlist.Name}'. " +
               $"Tracks: {shuffledItems.Count}, moved: {movedCount}, randomness: {randomnessLevel}.";
    }

    private sealed record PlaylistSource(
        List<Playlist> Playlists, bool IsSpotify, string? AccessToken, string? Error);
}
