using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Lets a signed-in user pick albums and bulk-add every track from them to one or more of their
// playlists. Serves two sources from the same UI: a Spotify-connected user searches the Spotify
// catalog and adds to their live Spotify playlists, while a local-account user searches the local
// album library and adds to local playlists. RequireServiceAuth gates the page; each action
// resolves the source from the request (Spotify session vs local account).
[Route("bulk-album-save")]
[RequireServiceAuth]
public class BulkAlbumSaveController : SpotifyControllerBase
{
    private readonly AlbumRepository _albumRepository;
    private readonly PlaylistRepository _playlistRepository;

    public BulkAlbumSaveController(
        SpotifyAuthService spotifyAuth,
        AlbumRepository albumRepository,
        PlaylistRepository playlistRepository) : base(spotifyAuth)
    {
        _albumRepository = albumRepository;
        _playlistRepository = playlistRepository;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!await IsSpotifyRequestAsync())
            return View(new BulkAlbumSavePage { Playlists = _playlistRepository.GetAll() });

        var accessToken = await GetSpotifyAccessTokenAsync();
        if (accessToken is null)
            return View(new BulkAlbumSavePage { ErrorMessage = MissingAccessTokenMessage });

        var playlists = await SpotifyAuth.GetUserPlaylistsAsync(accessToken);
        if (playlists is null)
            return View(new BulkAlbumSavePage { ErrorMessage = PlaylistsLoadFailedMessage });

        var profile = await SpotifyAuth.GetUserProfileAsync(accessToken);
        return View(new BulkAlbumSavePage { Playlists = FilterToOwnedPlaylists(playlists, profile) });
    }

    [HttpGet("search-albums")]
    public async Task<IActionResult> SearchAlbums(string? q)
    {
        var query = (q ?? string.Empty).Trim();
        if (query.Length < 2)
            return Json(Array.Empty<object>());

        if (!await IsSpotifyRequestAsync())
            return Json(SearchLocalAlbums(query));

        var accessToken = await GetSpotifyAccessTokenAsync();
        if (accessToken is null)
            return Json(Array.Empty<object>());

        var albums = await SpotifyAuth.SearchAlbumsAsync(accessToken, query);
        if (albums is null)
            return Json(Array.Empty<object>());

        var results = albums.Select(album => new
        {
            id = album.Id,
            name = album.Name,
            artists = string.Join(", ", album.Artists.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n))),
            albumType = album.AlbumType,
            releaseDate = album.ReleaseDate,
            totalTracks = album.TotalTracks,
            imageUrl = album.Images.OrderBy(i => i.Width ?? int.MaxValue).FirstOrDefault()?.Url,
            spotifyUrl = album.ExternalUrls.Spotify
        });

        return Json(results);
    }

    [HttpPost("confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm([FromForm] BulkAlbumSaveForm input)
    {
        if (!await IsSpotifyRequestAsync())
            return ConfirmLocal(input);

        var accessToken = await GetSpotifyAccessTokenAsync();
        if (accessToken is null)
            return Json(new BulkAddJsonResult(false, MissingAccessTokenMessage));

        var result = await SpotifyAuth.BulkSaveAlbumsToPlaylistsAsync(
            accessToken, input.AlbumIds, input.PlaylistIds);

        if (!result.Succeeded)
            return Json(new BulkAddJsonResult(false, result.Error ?? "Bulk add failed."));

        var message = $"Added {result.TrackCount} track(s) from {result.AlbumCount} album(s) to " +
            $"{result.PlaylistCount} playlist(s).";
        if (result.Warnings.Count > 0)
            message += " " + string.Join(" ", result.Warnings);

        return Json(new BulkAddJsonResult(true, message));
    }

    // Searches the local album library by name or artist, returning the same JSON shape the
    // Spotify search returns so the client picker is source-agnostic. The cover image, when one
    // was uploaded locally, is served through the Albums cover endpoint.
    private object[] SearchLocalAlbums(string query)
    {
        var needle = query.ToLowerInvariant();

        return _albumRepository.GetAll()
            .Where(album =>
                (album.Name ?? string.Empty).ToLowerInvariant().Contains(needle) ||
                album.Artists.Any(a => (a.Name ?? string.Empty).ToLowerInvariant().Contains(needle)))
            .Select(album => new
            {
                id = album.Id,
                name = album.Name,
                artists = string.Join(", ", album.Artists.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n))),
                albumType = album.AlbumType,
                releaseDate = album.ReleaseDate,
                totalTracks = album.TrackList.Count,
                imageUrl = album.HasCover
                    ? Url.Action("AlbumCover", "Albums", new { id = album.Id, v = album.CoverImageFileName })
                    : album.Images.OrderBy(i => i.Width ?? int.MaxValue).FirstOrDefault()?.Url,
                spotifyUrl = album.ExternalUrls?.Spotify ?? string.Empty
            })
            .Cast<object>()
            .ToArray();
    }

    // Adds every track from the selected local albums to every selected local playlist, skipping
    // tracks already present in a playlist (deduped per playlist, and across the chosen albums).
    private IActionResult ConfirmLocal(BulkAlbumSaveForm input)
    {
        if (input.AlbumIds.Count == 0 || input.PlaylistIds.Count == 0)
            return Json(new BulkAddJsonResult(false, "Select at least one album and one playlist."));

        var albums = input.AlbumIds
            .Select(id => _albumRepository.GetById(id))
            .OfType<Album>()
            .ToList();
        if (albums.Count == 0)
            return Json(new BulkAddJsonResult(false, "No valid albums selected."));

        var playlists = input.PlaylistIds
            .Select(id => _playlistRepository.GetById(id))
            .OfType<Playlist>()
            .ToList();
        if (playlists.Count == 0)
            return Json(new BulkAddJsonResult(false, "No valid playlists selected."));

        var albumTracks = albums.SelectMany(album => album.TrackList).ToList();
        var totalAdded = 0;

        foreach (var playlist in playlists)
        {
            // Seed with the playlist's current track ids; Add() returns false for ids already
            // present, so the same track is never added twice (whether already in the playlist
            // or duplicated across the selected albums).
            var seen = playlist.Tracks.Items.Select(item => item.Track.Id).ToHashSet();

            var added = 0;
            foreach (var track in albumTracks)
            {
                if (!seen.Add(track.Id))
                    continue;
                playlist.Tracks.Items.Add(new PlaylistTrack { Track = track });
                added++;
            }

            if (added == 0)
                continue;

            _playlistRepository.Save(playlist);
            totalAdded += added;
        }

        var message = $"Added {totalAdded} track(s) from {albums.Count} album(s) to {playlists.Count} playlist(s).";
        return Json(new BulkAddJsonResult(true, message));
    }

    private async Task<bool> IsSpotifyRequestAsync() =>
        (await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme)).Succeeded;

    private sealed record BulkAddJsonResult(bool Success, string Message);
}
