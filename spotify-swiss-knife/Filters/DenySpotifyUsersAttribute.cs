using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace spotify_swiss_knife.Filters;

// Blocks the local-library controllers for requests connected via Spotify. Spotify sessions
// and local accounts are mutually exclusive, and the library is local-account-only.
public sealed class DenySpotifyUsersAttribute : SpotifySessionFilterAttribute
{
    protected override IActionResult? Evaluate(ActionExecutingContext context, bool spotifyConnected) =>
        spotifyConnected
            ? AccessRestrictedResult.For(context,
                "Spotify session active",
                "This is part of the local library, which is only available to local accounts. " +
                "You're currently connected with Spotify, which unlocks the Spotify tools instead — " +
                "shuffling playlists, scheduled shuffles, and bulk album saving. Disconnect Spotify and " +
                "sign in with a local account to manage the library.")
            : null;
}
