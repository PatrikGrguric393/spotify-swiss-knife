using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace spotify_swiss_knife.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireSpotifyAuthAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var auth = await context.HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        if (!auth.Succeeded)
        {
            // A signed-in local account can never satisfy a Spotify requirement (the two are
            // mutually exclusive), so explain the conflict instead of bouncing it through the
            // Spotify login only to be rejected there. Anonymous visitors still go to login.
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = AccessRestrictedResult.For(context,
                    "Spotify connection required",
                    "This feature works directly with your Spotify account, but you're signed in with a " +
                    "local account. Local accounts and Spotify are mutually exclusive — log out of your " +
                    "local account, then connect Spotify to continue.");
                return;
            }

            var returnUrl = context.HttpContext.Request.Path.Value;
            context.Result = new RedirectToActionResult("Login", "SpotifyAuth", new { returnUrl });
            return;
        }

        await next();
    }
}
