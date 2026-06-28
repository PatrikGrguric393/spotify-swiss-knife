using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Identity for these controllers lives in the SpotifyConnect auth cookie rather than the default
// Identity cookie, so it must be read explicitly via AuthenticateAsync.
public abstract class SpotifyControllerBase : Controller
{
    protected SpotifyControllerBase(SpotifyAuthService spotifyAuth)
    {
        SpotifyAuth = spotifyAuth;
    }

    protected SpotifyAuthService SpotifyAuth { get; }

    protected async Task<string?> GetSpotifyUserIdAsync()
    {
        var auth = await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        return auth.Succeeded ? auth.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) : null;
    }

    // The token in the auth cookie is frozen at sign-in and goes stale after ~1 hour; this
    // resolves a live token from the DB-backed store, refreshing it when needed.
    protected async Task<string?> GetSpotifyAccessTokenAsync()
    {
        var userId = await GetSpotifyUserIdAsync();
        return string.IsNullOrEmpty(userId)
            ? null
            : await SpotifyAuth.GetValidAccessTokenAsync(userId);
    }

    protected async Task<(string? UserId, string? AccessToken)> GetSpotifyCredentialsAsync()
    {
        var userId = await GetSpotifyUserIdAsync();
        if (string.IsNullOrEmpty(userId))
            return (null, null);

        return (userId, await SpotifyAuth.GetValidAccessTokenAsync(userId));
    }

    protected IActionResult RedirectToSpotifyLogin(string? returnUrl = null) =>
        RedirectToAction("Login", "SpotifyAuth", returnUrl is null ? null : new { returnUrl });

    protected const string MissingAccessTokenMessage =
        "Your Spotify connection is missing an access token. Please disconnect and reconnect Spotify.";

    protected const string PlaylistsLoadFailedMessage =
        "We couldn't load your Spotify playlists. Your session may have expired or lacks the required " +
        "permission — please disconnect and reconnect Spotify.";

    // Returns unfiltered rather than empty when profile couldn't be resolved, so a transient
    // profile-fetch failure doesn't make the user's own playlists disappear.
    protected static List<Playlist> FilterToOwnedPlaylists(List<Playlist> playlists, SpotifyUserProfile? profile) =>
        profile is not null
            ? playlists.Where(p => p.Owner.Id == profile.Id).ToList()
            : playlists;
}
