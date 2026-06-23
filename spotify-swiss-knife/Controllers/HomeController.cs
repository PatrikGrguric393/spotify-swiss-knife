using Microsoft.AspNetCore.Mvc;

namespace spotify_swiss_knife.Controllers;

// Serves the application landing page at the site root ("/").
public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
    }
}
