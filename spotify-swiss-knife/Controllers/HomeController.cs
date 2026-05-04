using Microsoft.AspNetCore.Mvc;

namespace spotify_swiss_knife.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
    }
}
