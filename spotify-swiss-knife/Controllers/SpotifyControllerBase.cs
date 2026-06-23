using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Base class for the MVC controllers that act on the signed-in user's live Spotify account
// (shuffle, scheduling, bulk album save). It centralises identity and access-token resolution
// so each controller doesn't re-implement the same cookie lookup and token refresh.
//
// Identity for these controllers lives in the dedicated SpotifyConnect auth cookie rather than
// the default Identity cookie, so it must be read explicitly via AuthenticateAsync.
public abstract class SpotifyControllerBase : Controller
{
    protected SpotifyControllerBase(SpotifyAuthService spotifyAuth)
    {
        SpotifyAuth = spotifyAuth;
    }

    // Exposed to subclasses for the calls that aren't about token resolution
    // (fetching playlists, searching albums, running a shuffle, etc.).
    protected SpotifyAuthService SpotifyAuth { get; }

    // The Spotify user id from the connection cookie, or null when Spotify isn't connected.
    protected async Task<string?> GetSpotifyUserIdAsync()
    {
        var auth = await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        return auth.Succeeded ? auth.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) : null;
    }

    // Resolves a live token from the DB-backed store, which refreshes it when the short-lived
    // Spotify access token has expired. The token embedded in the auth cookie is frozen at
    // sign-in and goes stale after ~1 hour, even though the cookie session itself stays valid
    // for days. Returns null when Spotify isn't connected or no token can be resolved.
    protected async Task<string?> GetSpotifyAccessTokenAsync()
    {
        var userId = await GetSpotifyUserIdAsync();
        return string.IsNullOrEmpty(userId)
            ? null
            : await SpotifyAuth.GetValidAccessTokenAsync(userId);
    }

    // Both the user id and a live access token in a single cookie read. Either element is null
    // when Spotify isn't connected or no token can be resolved.
    protected async Task<(string? UserId, string? AccessToken)> GetSpotifyCredentialsAsync()
    {
        var userId = await GetSpotifyUserIdAsync();
        if (string.IsNullOrEmpty(userId))
            return (null, null);

        return (userId, await SpotifyAuth.GetValidAccessTokenAsync(userId));
    }

    // Shown when the connection cookie is still valid but no live access token could be resolved
    // (e.g. the refresh token was revoked on Spotify's side).
    protected const string MissingAccessTokenMessage =
        "Your Spotify connection is missing an access token. Please disconnect and reconnect Spotify.";

    // Shown when the playlists request itself fails — typically an expired session or a missing
    // OAuth scope.
    protected const string PlaylistsLoadFailedMessage =
        "We couldn't load your Spotify playlists. Your session may have expired or lacks the required " +
        "permission — please disconnect and reconnect Spotify.";

    // Restricts a playlist list to the ones the signed-in user owns. Only owned playlists can be
    // reordered or modified on Spotify, so the others are hidden from every picker. When the
    // profile couldn't be resolved the list is returned unfiltered rather than emptied, so a
    // transient profile-fetch failure doesn't make the user's playlists disappear.
    protected static List<Playlist> FilterToOwnedPlaylists(List<Playlist> playlists, SpotifyUserProfile? profile) =>
        profile is not null
            ? playlists.Where(p => p.Owner.Id == profile.Id).ToList()
            : playlists;
}
