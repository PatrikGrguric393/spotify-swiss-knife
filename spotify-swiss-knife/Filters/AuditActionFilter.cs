using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace spotify_swiss_knife.Filters;

// Global filter that records every controller action invocation. Mutating verbs
// (POST/PUT/PATCH/DELETE) are logged at Information to capture all CRUD and
// Services-page usage; reads are logged at Debug since HTTP logging already covers them.
public sealed class AuditActionFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private readonly ILogger<AuditActionFilter> _logger;

    public AuditActionFilter(ILogger<AuditActionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var method = http.Request.Method;
        var isMutation = MutatingMethods.Contains(method);
        var level = isMutation ? LogLevel.Information : LogLevel.Debug;

        if (!_logger.IsEnabled(level))
        {
            await next();
            return;
        }

        var descriptor = context.ActionDescriptor.RouteValues;
        var controller = descriptor.TryGetValue("controller", out var c) ? c : "?";
        var action = descriptor.TryGetValue("action", out var a) ? a : "?";
        var user = await ResolveUserAsync(http);

        _logger.Log(level, "Action {Method} {Controller}/{Action} invoked by {User}.",
            method, controller, action, user);

        var executed = await next();

        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            _logger.LogError(executed.Exception,
                "Action {Controller}/{Action} for {User} threw an exception.", controller, action, user);
        }
        else
        {
            _logger.Log(level, "Action {Controller}/{Action} for {User} returned {Result}.",
                controller, action, user, executed.Result?.GetType().Name ?? "null");
        }
    }

    // Resolves the acting user across both auth schemes: the default Identity cookie for
    // local accounts, and the separate Spotify cookie scheme for connected Spotify users.
    private static async Task<string> ResolveUserAsync(HttpContext http)
    {
        if (http.User.Identity?.IsAuthenticated == true)
            return http.User.Identity.Name ?? http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "authenticated";

        var spotify = await http.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        if (spotify.Succeeded)
            return spotify.Principal?.Identity?.Name ?? "spotify-user";

        return "anonymous";
    }
}
