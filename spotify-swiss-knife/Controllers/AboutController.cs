using Microsoft.AspNetCore.Mvc;

namespace spotify_swiss_knife.Controllers;

public class AboutController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
