using Microsoft.AspNetCore.Mvc;

namespace spotify_swiss_knife.Controllers;

[Route("lib")]
public class LibraryController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return Redirect("/lib/songs");
    }
}
