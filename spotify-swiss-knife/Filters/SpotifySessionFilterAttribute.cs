using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace spotify_swiss_knife.Filters;

// Shared base for the action filters that gate an action on whether the current request
// carries an active Spotify session (DenySpotifyUsers / RequireSpotifyAuth).
//
// This base reads the Spotify session once and handles the short-circuit plumbing;
// subclasses only decide the policy by implementing Evaluate.
//
// The AttributeUsage is inherited by the subclasses, so they don't repeat it: both gate
// either a whole controller ([class]) or a single action ([method]).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class SpotifySessionFilterAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var spotifyConnected = await context.HttpContext.IsSpotifyConnectedAsync();

        var blocked = Evaluate(context, spotifyConnected);
        if (blocked is not null)
        {
            context.Result = blocked;
            return;
        }

        await next();
    }

    // Returns a result to short-circuit the request (e.g. a redirect or an AccessRestricted
    // view), or null to let the action run.
    protected abstract IActionResult? Evaluate(ActionExecutingContext context, bool spotifyConnected);
}
