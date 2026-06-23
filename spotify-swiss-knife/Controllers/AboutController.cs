using Microsoft.AspNetCore.Mvc;

namespace spotify_swiss_knife.Controllers;

// Serves the static "About" page at /about.
public class AboutController : Controller
{
    [HttpGet("/about")]
    public IActionResult Index()
    {
        return View();
    }
}
