using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace spotify_swiss_knife.Filters;

// Requires an active Spotify connection for the Spotify-only tools (shuffle, scheduling,
// bulk album save).
public sealed class RequireSpotifyAuthAttribute : SpotifySessionFilterAttribute
{
    protected override IActionResult? Evaluate(ActionExecutingContext context, bool spotifyConnected)
    {
        if (spotifyConnected)
            return null;

        // A signed-in local account can never satisfy a Spotify requirement (the two are
        // mutually exclusive), so explain the conflict instead of bouncing it through the
        // Spotify login only to be rejected there. Anonymous visitors still go to login.
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            return AccessRestrictedResult.For(context,
                "Spotify connection required",
                "This feature works directly with your Spotify account, but you're signed in with a " +
                "local account. Local accounts and Spotify are mutually exclusive — log out of your " +
                "local account, then connect Spotify to continue.");
        }

        var returnUrl = context.HttpContext.Request.Path.Value;
        return new RedirectToActionResult("Login", "SpotifyAuth", new { returnUrl });
    }
}
