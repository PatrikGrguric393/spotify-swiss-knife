using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[Route("lib")]
[Authorize(Roles = "Admin,Editor")]
public class ArtistsController : Controller
{
    private readonly ArtistRepository _artistRepository;

    public ArtistsController(ArtistRepository artistRepository)
    {
        _artistRepository = artistRepository;
    }

    [AllowAnonymous]
    [HttpGet("artists")]
    public IActionResult Artists()
    {
        return View(_artistRepository.GetAll());
    }

    [HttpGet("artists/create")]
    public IActionResult CreateArtist()
    {
        return View();
    }

    [HttpPost("artists/create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreateArtist([FromForm] Models.FormModels.ArtistCreateModel model)
    {
        if (!ModelState.IsValid)
            return View("CreateArtist", model);

        if (!string.IsNullOrEmpty(model.SpotifyUrl) &&
            (!Uri.TryCreate(model.SpotifyUrl, UriKind.Absolute, out var tmp) || !tmp.Host.Contains("spotify.com")))
        {
            ModelState.AddModelError("SpotifyUrl", "Spotify URL must be a valid spotify.com link.");
            return View("CreateArtist", model);
        }

        if (_artistRepository.ExistsByName(model.Name))
        {
            ModelState.AddModelError("Name", $"An artist named '{(model.Name ?? string.Empty).Trim()}' already exists.");
            return View("CreateArtist", model);
        }

        _artistRepository.Add(new Models.Artist
        {
            Id = Guid.NewGuid().ToString(),
            Name = model.Name,
            ExternalUrls = new Models.ExternalUrls { Spotify = (model.SpotifyUrl ?? string.Empty).Trim() }
        });

        return RedirectToAction(nameof(Artists));
    }

    [HttpGet("artists/edit/{id}")]
    public IActionResult EditArtist(string id)
    {
        var artist = _artistRepository.GetById(id);
        if (artist is null) return NotFound();

        return View(new Models.FormModels.ArtistEditModel
        {
            Id = artist.Id,
            Name = artist.Name ?? string.Empty,
            SpotifyUrl = artist.ExternalUrls?.Spotify ?? string.Empty
        });
    }

    [HttpPost("artists/edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditArtistPost(string id, [FromForm] Models.FormModels.ArtistEditModel model)
    {
        var artist = _artistRepository.GetById(id, includeDeleted: true);
        if (artist is null) return NotFound();

        if (!ModelState.IsValid)
            return View("EditArtist", model);

        if (!string.IsNullOrEmpty(model.SpotifyUrl) &&
            (!Uri.TryCreate(model.SpotifyUrl, UriKind.Absolute, out var tmp) || !tmp.Host.Contains("spotify.com")))
        {
            ModelState.AddModelError("SpotifyUrl", "Spotify URL must be a valid spotify.com link.");
            return View("EditArtist", model);
        }

        if (_artistRepository.ExistsByName(model.Name, id))
        {
            ModelState.AddModelError("Name", $"An artist named '{(model.Name ?? string.Empty).Trim()}' already exists.");
            return View("EditArtist", model);
        }

        artist.Name = model.Name.Trim();
        artist.ExternalUrls ??= new Models.ExternalUrls();
        artist.ExternalUrls.Spotify = (model.SpotifyUrl ?? string.Empty).Trim();

        _artistRepository.Update(artist);
        return RedirectToAction(nameof(Artists));
    }

    [HttpGet("artists/delete/{id}")]
    public IActionResult DeleteArtist(string id)
    {
        var artist = _artistRepository.GetById(id);
        if (artist is null) return NotFound();
        return View(artist);
    }

    [HttpPost("artists/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteArtistConfirmed(string id)
    {
        _artistRepository.SoftDelete(id);
        return RedirectToAction(nameof(Artists));
    }

    [AllowAnonymous]
    [HttpGet("artists/search")]
    public IActionResult SearchArtists(string q)
    {
        var all = _artistRepository.GetAll();
        var results = string.IsNullOrWhiteSpace(q)
            ? all.Take(20)
            : all.Where(a => a.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(20);

        return Json(results.Select(a => new { a.Id, a.Name, SpotifyUrl = a.ExternalUrls?.Spotify }).ToList());
    }

    [AllowAnonymous]
    [HttpGet("artists/validate-name")]
    public IActionResult ValidateArtistName(string q, string? excludeId)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new { isUnique = false });
        return Json(new { isUnique = !_artistRepository.ExistsByName(q, excludeId) });
    }
}
