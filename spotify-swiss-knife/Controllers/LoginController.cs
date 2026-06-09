using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace spotify_swiss_knife.Controllers;

[Route("login")]
public class LoginController : Controller
{
    // Landing page where the user picks a single login method. Local accounts and
    // Spotify are mutually exclusive, so anyone already signed in by either method
    // skips the chooser entirely.
    [HttpGet("")]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        var spotifyConnected = (await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme)).Succeeded;
        if (User.Identity?.IsAuthenticated == true || spotifyConnected)
            return LocalRedirect(returnUrl ?? "/");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
}
