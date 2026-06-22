using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace spotify_swiss_knife.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DenySpotifyUsersAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var auth = await context.HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        if (auth.Succeeded)
        {
            context.Result = AccessRestrictedResult.For(context,
                "Spotify session active",
                "This is part of the local library, which is only available to local accounts. " +
                "You're currently connected with Spotify, which unlocks the Spotify tools instead — " +
                "shuffling playlists, scheduled shuffles, and bulk album saving. Disconnect Spotify and " +
                "sign in with a local account to manage the library.");
            return;
        }

        await next();
    }
}
