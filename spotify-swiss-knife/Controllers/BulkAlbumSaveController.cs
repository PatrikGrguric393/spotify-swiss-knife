using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[Route("bulk-album-save")]
[RequireSpotifyAuth]
public class BulkAlbumSaveController : Controller
{
    private readonly SpotifyAuthService _spotifyAuth;

    public BulkAlbumSaveController(SpotifyAuthService spotifyAuth)
    {
        _spotifyAuth = spotifyAuth;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var accessToken = await GetAccessTokenAsync();
        if (accessToken is null)
            return View(new BulkAlbumSavePage { ErrorMessage = MissingTokenMessage });

        var playlists = await _spotifyAuth.GetUserPlaylistsAsync(accessToken);
        if (playlists is null)
        {
            return View(new BulkAlbumSavePage
            {
                ErrorMessage = "We couldn't load your Spotify playlists. Your session may have expired or " +
                    "lacks the required permission — please disconnect and reconnect Spotify."
            });
        }

        var profile = await _spotifyAuth.GetUserProfileAsync(accessToken);
        var editablePlaylists = profile is not null
            ? playlists.Where(p => p.Owner.Id == profile.Id).ToList()
            : playlists;

        return View(new BulkAlbumSavePage { Playlists = editablePlaylists });
    }

    [HttpGet("search-albums")]
    public async Task<IActionResult> SearchAlbums(string? q)
    {
        var query = (q ?? string.Empty).Trim();
        if (query.Length < 2)
            return Json(Array.Empty<object>());

        var accessToken = await GetAccessTokenAsync();
        if (accessToken is null)
            return Json(Array.Empty<object>());

        var albums = await _spotifyAuth.SearchAlbumsAsync(accessToken, query);
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
        var accessToken = await GetAccessTokenAsync();
        if (accessToken is null)
            return Json(new BulkAddJsonResult(false, MissingTokenMessage));

        var result = await _spotifyAuth.BulkSaveAlbumsToPlaylistsAsync(
            accessToken, input.AlbumIds, input.PlaylistIds);

        if (!result.Succeeded)
            return Json(new BulkAddJsonResult(false, result.Error ?? "Bulk add failed."));

        var message = $"Added {result.TrackCount} track(s) from {result.AlbumCount} album(s) to " +
            $"{result.PlaylistCount} playlist(s).";
        if (result.Warnings.Count > 0)
            message += " " + string.Join(" ", result.Warnings);

        return Json(new BulkAddJsonResult(true, message));
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        var auth = await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        var token = auth.Principal?.FindFirst("access_token")?.Value;
        return string.IsNullOrEmpty(token) ? null : token;
    }

    private const string MissingTokenMessage =
        "Your Spotify connection is missing an access token. Please disconnect and reconnect Spotify.";

    private sealed record BulkAddJsonResult(bool Success, string Message);
}
