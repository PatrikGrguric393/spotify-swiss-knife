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
            context.Result = new RedirectToActionResult("Index", "Home", null);
            return;
        }

        await next();
    }
}
