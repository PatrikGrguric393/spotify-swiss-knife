using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Filters;

namespace spotify_swiss_knife.Controllers;

[Route("lib")]
[DenySpotifyUsers]
public class LibraryController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return Redirect("/lib/tracks");
    }
}
