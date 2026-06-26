namespace spotify_swiss_knife.Infrastructure;

// Resolves the page a user came from so an access-denied screen can send them "back" there
// instead of dumping them on the home page. Used by both denial surfaces: the cookie
// AccessDeniedPath view and the Spotify/local conflict view.
public static class PreviousPage
{
    private const string Home = "/";

    // Derives a safe "back" target from the Referer header. Only the path+query of a
    // same-host referrer is honoured, and the result is always relative, so it can never be
    // turned into an open redirect to another site. The denied pages themselves are excluded
    // so "go back" can't loop the user straight into another refusal; anything unusable
    // falls back to the home page.
    public static string ResolveBackUrl(HttpContext context)
    {
        var referer = context.Request.Headers.Referer.ToString();
        if (string.IsNullOrEmpty(referer))
            return Home;

        if (!Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            return Home;

        if (!uri.Host.Equals(context.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            return Home;

        var path = uri.PathAndQuery;
        if (path.StartsWith("/account/denied", StringComparison.OrdinalIgnoreCase))
            return Home;

        return path;
    }
}
