using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Infrastructure;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Server-rendered CRUD for the local library's artists, under /lib/artists. Listing is public,
// but creating, editing, and deleting require an Admin or Editor local account (deletes are
// Admin-only and soft, see ArtistRepository.SoftDelete). DenySpotifyUsers keeps Spotify-connected
// visitors out of the local library entirely. The JSON CRUD counterpart is ArtistsApiController.
[Route("lib")]
[Authorize(Roles = "Admin,Editor")]
[DenySpotifyUsers]
public class ArtistsController : Controller
{
    private readonly ArtistRepository _artistRepository;

    public ArtistsController(ArtistRepository artistRepository)
    {
        _artistRepository = artistRepository;
    }

    [AllowAnonymous]
    [HttpGet("artists")]
    public IActionResult Index()
    {
        return View(_artistRepository.GetAll());
    }

    [HttpGet("artists/create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("artists/create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePost([FromForm] Models.FormModels.ArtistCreateForm model)
    {
        if (!ModelState.IsValid)
            return View("Create", model);

        if (!TryValidateSpotifyUrl(model.SpotifyUrl))
            return View("Create", model);

        if (_artistRepository.ExistsByName(model.Name))
        {
            ModelState.AddModelError("Name", $"An artist named '{(model.Name ?? string.Empty).Trim()}' already exists.");
            return View("Create", model);
        }

        _artistRepository.Add(new Models.Artist
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = model.Name.Trim(),
            ExternalUrls = new Models.ExternalUrls { Spotify = (model.SpotifyUrl ?? string.Empty).Trim() }
        });

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("artists/edit/{id}")]
    public IActionResult Edit(string id)
    {
        var artist = _artistRepository.GetById(id);
        if (artist is null) return NotFound();

        return View(new Models.FormModels.ArtistEditForm
        {
            Id = artist.Id,
            Name = artist.Name ?? string.Empty,
            SpotifyUrl = artist.ExternalUrls?.Spotify ?? string.Empty
        });
    }

    [HttpPost("artists/edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditPost(string id, [FromForm] Models.FormModels.ArtistEditForm model)
    {
        var artist = _artistRepository.GetById(id, includeDeleted: true);
        if (artist is null) return NotFound();

        if (!ModelState.IsValid)
            return View("Edit", model);

        if (!TryValidateSpotifyUrl(model.SpotifyUrl))
            return View("Edit", model);

        if (_artistRepository.ExistsByName(model.Name, id))
        {
            ModelState.AddModelError("Name", $"An artist named '{(model.Name ?? string.Empty).Trim()}' already exists.");
            return View("Edit", model);
        }

        artist.Name = model.Name.Trim();
        artist.ExternalUrls ??= new Models.ExternalUrls();
        artist.ExternalUrls.Spotify = (model.SpotifyUrl ?? string.Empty).Trim();

        _artistRepository.Update(artist);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("artists/delete/{id}")]
    public IActionResult Delete(string id)
    {
        var artist = _artistRepository.GetById(id);
        if (artist is null) return NotFound();
        return View(artist);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("artists/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(string id)
    {
        _artistRepository.SoftDelete(id);
        return RedirectToAction(nameof(Index));
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

    // Validates the optional Spotify URL, recording a model error for redisplay when invalid.
    private bool TryValidateSpotifyUrl(string? url)
    {
        if (SpotifyUrl.IsValid(url)) return true;
        ModelState.AddModelError("SpotifyUrl", SpotifyUrl.ValidationMessage);
        return false;
    }
}
