using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace spotify_swiss_knife;

internal static class SpotifyAuthDefaults
{
    internal const string Scheme = "SpotifyConnect";
}

internal static class SpotifyAuthExtensions
{
    // Spotify identity lives in the dedicated SpotifyConnect cookie rather than the default
    // Identity cookie, so it has to be read explicitly with AuthenticateAsync. This wraps the
    // probe that several filters and controllers would otherwise repeat verbatim.
    public static async Task<bool> IsSpotifyConnectedAsync(this HttpContext context) =>
        (await context.AuthenticateAsync(SpotifyAuthDefaults.Scheme)).Succeeded;
}
