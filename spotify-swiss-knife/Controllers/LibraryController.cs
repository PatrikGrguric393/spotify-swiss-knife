using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Filters;

namespace spotify_swiss_knife.Controllers;

// Entry point for the local library at /lib. The library has no landing page of its own, so it
// redirects to the tracks listing. DenySpotifyUsers keeps Spotify-connected visitors out of the
// local library (its individual sections live in Tracks/Albums/Artists/Playlists controllers).
[Route("lib")]
[DenySpotifyUsers]
public class LibraryController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Tracks");
    }
}
