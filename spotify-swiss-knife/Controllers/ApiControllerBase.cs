using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Infrastructure;

namespace spotify_swiss_knife.Controllers;

// Pinning the JWT bearer scheme here means [Authorize(Roles = ...)] on actions evaluates the
// bearer identity rather than the Identity cookie used by the MVC app.
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult? SpotifyUrlValidationProblem(string? url)
    {
        if (SpotifyUrl.IsValid(url)) return null;
        ModelState.AddModelError("SpotifyUrl", SpotifyUrl.ValidationMessage);
        return ValidationProblem(ModelState);
    }

    // 404 when any requested related id has no matching row, so unknown references fail loudly
    // instead of being silently dropped.
    protected NotFoundObjectResult? MissingReferenceProblem(
        IEnumerable<string> requestedIds, IEnumerable<string> foundIds, string entityName)
    {
        var missing = requestedIds.Except(foundIds).ToList();
        if (missing.Count == 0) return null;
        return NotFound(new { message = $"{entityName}(s) not found: {string.Join(", ", missing)}." });
    }

    protected static IEnumerable<T> ApplySearchFilter<T>(
        IEnumerable<T> items, string? query, Func<T, string> idSelector, Func<T, string> nameSelector)
    {
        if (string.IsNullOrWhiteSpace(query))
            return items;

        var term = query.Trim();
        return items
            .Where(item => idSelector(item).Equals(term, StringComparison.OrdinalIgnoreCase)
                           || nameSelector(item).Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
