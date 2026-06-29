using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Shuffles the track order of one or more playlists. It serves two distinct sources from the
// same UI: a Spotify-connected user shuffles their own Spotify playlists (changes are pushed to
// Spotify), while a local-account user shuffles the local library's playlists in the database.
// RequireSpotifyAuth gates the page, and ResolvePlaylistsAsync decides which source applies.
[Route("shuffle")]
[RequireServiceAuth]
public class ShuffleController : SpotifyControllerBase
{
    private readonly PlaylistRepository _playlistRepository;
    private readonly ILogger<ShuffleController> _logger;

    public ShuffleController(
        PlaylistRepository playlistRepository,
        SpotifyAuthService spotifyAuth,
        ILogger<ShuffleController> logger) : base(spotifyAuth)
    {
        _playlistRepository = playlistRepository;
        _logger = logger;
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

        if (input.PlaylistIds.Count == 0)
            return Json(new ShuffleJsonResult(false, "Please select at least one playlist.", null));

        var selected = source.Playlists
            .Where(p => input.PlaylistIds.Contains(p.Id))
            .ToList();

        if (selected.Count == 0)
            return Json(new ShuffleJsonResult(false, "No valid playlists selected.", null));

        if (source.IsSpotify)
            return await ShuffleSpotifyPlaylistsJsonAsync(source, selected);

        var (message, shuffledAt) = ExecuteShuffleMultiple(selected);
        return Json(new ShuffleJsonResult(true, message, shuffledAt.ToString("o")));
    }

    private async Task<IActionResult> ShuffleSpotifyPlaylistsJsonAsync(
        PlaylistSource source, List<Playlist> playlists)
    {
        var errors = new List<string>();
        int totalTracks = 0, totalMoved = 0;

        foreach (var playlist in playlists)
        {
            var result = await SpotifyAuth.ShufflePlaylistAsync(
                source.AccessToken!, playlist.Id);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Spotify shuffle failed for playlist {PlaylistId} ({Name}): {Error}.",
                    playlist.Id, playlist.Name, result.Error);
                errors.Add($"'{playlist.Name}': {result.Error}");
                continue;
            }

            totalTracks += result.TrackCount;
            totalMoved += result.MovedCount;
            _logger.LogInformation(
                "Spotify shuffle completed for playlist {PlaylistId} ({Name}). Tracks: {Tracks}, moved: {Moved}.",
                playlist.Id, playlist.Name, result.TrackCount, result.MovedCount);
        }

        if (errors.Count == playlists.Count)
            return Json(new ShuffleJsonResult(false, errors[0].Split(": ", 2).ElementAtOrDefault(1) ?? errors[0], null));

        var message = playlists.Count == 1
            ? $"Shuffle completed for '{playlists[0].Name}'. Tracks: {totalTracks}, moved: {totalMoved}."
            : $"Shuffled {playlists.Count - errors.Count} of {playlists.Count} playlists. Tracks: {totalTracks}, moved: {totalMoved}.";

        if (errors.Count > 0)
            message += $" Failed: {string.Join("; ", errors)}";

        return Json(new ShuffleJsonResult(true, message, DateTime.UtcNow.ToString("o")));
    }

    private (string Message, DateTime ShuffledAt) ExecuteShuffleMultiple(List<Playlist> playlists)
    {
        int totalTracks = 0, totalMoved = 0;
        var shuffledAt = DateTime.UtcNow;

        foreach (var playlist in playlists)
        {
            var result = LocalPlaylistShuffle.ShuffleAndSave(_playlistRepository, playlist);
            totalTracks += result.TrackCount;
            totalMoved += result.MovedCount;
            shuffledAt = result.ShuffledAt;

            _logger.LogInformation(
                "Local shuffle completed for playlist {PlaylistId} ({Name}). Tracks: {Tracks}, moved: {Moved}.",
                playlist.Id, playlist.Name, result.TrackCount, result.MovedCount);
        }

        var message = playlists.Count == 1
            ? $"Shuffle completed for '{playlists[0].Name}'. Tracks: {totalTracks}, moved: {totalMoved}."
            : $"Shuffled {playlists.Count} playlists. Tracks: {totalTracks}, moved: {totalMoved}.";

        return (message, shuffledAt);
    }

    private sealed record ShuffleJsonResult(bool Success, string Message, string? ShuffledAtUtc);

    private async Task<PlaylistSource> ResolvePlaylistsAsync()
    {
        var spotifyAuth = await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        if (spotifyAuth.Succeeded)
        {
            var accessToken = await GetSpotifyAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
                return new PlaylistSource([], true, null, MissingAccessTokenMessage);

            var playlists = await SpotifyAuth.GetUserPlaylistsAsync(accessToken);
            if (playlists is null)
                return new PlaylistSource([], true, null, PlaylistsLoadFailedMessage);

            var profile = await SpotifyAuth.GetUserProfileAsync(accessToken);
            return new PlaylistSource(FilterToOwnedPlaylists(playlists, profile), true, accessToken, null);
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            return new PlaylistSource(_playlistRepository.GetAll(), false, null, null);
        }

        return new PlaylistSource([], false, null,
            "You're not logged in. Connect Spotify or sign in with a local account to load playlists to shuffle.");
    }

    private sealed record PlaylistSource(
        List<Playlist> Playlists, bool IsSpotify, string? AccessToken, string? Error);
}
