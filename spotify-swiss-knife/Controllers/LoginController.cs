using Microsoft.AspNetCore.Mvc;

namespace spotify_swiss_knife.Controllers;

// Hosts the login chooser at /login, where a visitor picks one authentication method. Local
// accounts (AccountController) and Spotify (SpotifyAuthController) are mutually exclusive, so a
// visitor already signed in by either method skips the chooser.
[Route("login")]
public class LoginController : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        var spotifyConnected = await HttpContext.IsSpotifyConnectedAsync();
        if (User.Identity?.IsAuthenticated == true || spotifyConnected)
            return LocalRedirect(returnUrl ?? "/");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
}
