using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using spotify_swiss_knife.Infrastructure;

namespace spotify_swiss_knife.Filters;

// Global filter that records every controller action invocation. Mutating verbs
// (POST/PUT/PATCH/DELETE) are logged at Information to capture all CRUD and
// Services-page usage; reads are logged at Debug since HTTP logging already covers them.
//
// It opens a logging scope ({RequestId}, {User}) around the action so the controller's own
// log lines can be correlated to the request and actor without each one repeating them. It
// does not log a per-action result line — HttpLogging already records the status and duration
// of every response, so duplicating that here would only add noise.
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

        // Exceptions are always logged; only skip the work when neither the audit line nor an
        // error line could be emitted.
        if (!_logger.IsEnabled(level) && !_logger.IsEnabled(LogLevel.Error))
        {
            await next();
            return;
        }

        var descriptor = context.ActionDescriptor.RouteValues;
        var controller = descriptor.TryGetValue("controller", out var c) ? c : "?";
        var action = descriptor.TryGetValue("action", out var a) ? a : "?";
        var user = LogScrub.User(await ResolveUserAsync(http));

        // Correlate every log written during this action to the request and the acting user.
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = http.TraceIdentifier,
            ["User"] = user,
        });

        _logger.Log(level, "Action {Method} {Controller}/{Action} invoked by {User}.",
            method, controller, action, user);

        var executed = await next();

        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            _logger.LogError(executed.Exception,
                "Action {Controller}/{Action} for {User} threw an exception.", controller, action, user);
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
