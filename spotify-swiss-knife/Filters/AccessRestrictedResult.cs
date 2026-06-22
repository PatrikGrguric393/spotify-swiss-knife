using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Filters;

internal static class AccessRestrictedResult
{
    // Renders the shared AccessRestricted view with a 403 status instead of issuing a
    // silent redirect, so the user is told why the wrong session type was refused.
    public static ViewResult For(ActionExecutingContext context, string heading, string message)
    {
        var metadataProvider = context.HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
        return new ViewResult
        {
            ViewName = "AccessRestricted",
            StatusCode = StatusCodes.Status403Forbidden,
            ViewData = new ViewDataDictionary<AccessRestrictedViewModel>(metadataProvider, context.ModelState)
            {
                Model = new AccessRestrictedViewModel { Heading = heading, Message = message },
            },
        };
    }
}
