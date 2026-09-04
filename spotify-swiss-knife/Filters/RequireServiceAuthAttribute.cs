using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace spotify_swiss_knife.Filters;

// Gates the service tools (shuffle, scheduling, bulk album save) on being signed in by EITHER
// method: a Spotify connection operates on the live Spotify account, while a local account
// operates on the local library. Only fully anonymous visitors are bounced — to the login
// chooser, where they pick a method. Each gated action decides the data source for itself.
public sealed class RequireServiceAuthAttribute : SpotifySessionFilterAttribute
{
    protected override IActionResult? Evaluate(ActionExecutingContext context, bool spotifyConnected)
    {
        if (spotifyConnected || context.HttpContext.User.Identity?.IsAuthenticated == true)
            return null;

        var returnUrl = context.HttpContext.Request.Path.Value;
        return new RedirectToActionResult("Index", "Login", new { returnUrl });
    }
}
