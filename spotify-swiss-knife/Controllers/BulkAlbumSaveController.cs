using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Lets a Spotify-connected user pick albums and bulk-add every track from them to one or more
// of their own playlists. All actions require a live Spotify connection (see RequireSpotifyAuth)
// and operate on the user's live Spotify account, not the local library.
[Route("bulk-album-save")]
[RequireSpotifyAuth]
public class BulkAlbumSaveController : SpotifyControllerBase
{
    public BulkAlbumSaveController(SpotifyAuthService spotifyAuth) : base(spotifyAuth)
    {
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
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

    private sealed record BulkAddJsonResult(bool Success, string Message);
}
