using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[Route("lib")]
public class LibraryController : Controller
{
    private readonly TrackRepository _trackRepository;
    private readonly AlbumRepository _albumRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly PlaylistRepository _playlistRepository;

    public LibraryController(
        TrackRepository trackRepository,
        AlbumRepository albumRepository,
        ArtistRepository artistRepository,
        PlaylistRepository playlistRepository)
    {
        _trackRepository = trackRepository;
        _albumRepository = albumRepository;
        _artistRepository = artistRepository;
        _playlistRepository = playlistRepository;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Songs));
    }

    [HttpGet("songs")]
    public IActionResult Songs()
    {
        var songs = _trackRepository.GetAll();
        return View(songs);
    }

    [HttpGet("albums")]
    public IActionResult Albums()
    {
        var albums = _albumRepository.GetAll();
        return View(albums);
    }

    [HttpGet("artists")]
    public IActionResult Artists()
    {
        var artists = _artistRepository.GetAll();
        return View(artists);
    }

    [HttpGet("artists/create")]
    public IActionResult CreateArtist()
    {
        return View("Create");
    }

    [HttpPost("artists/create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreateArtist([FromForm] Models.FormModels.ArtistCreateModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Create", model);
        }

        var artist = new Models.Artist
        {
            Id = Guid.NewGuid().ToString(),
            Name = model.Name,
            ExternalUrls = new Models.ExternalUrls { Spotify = string.Empty }
        };

        _artistRepository.Add(artist);
        return RedirectToAction(nameof(Artists));
    }

    [HttpGet("artists/edit/{id}")]
    public IActionResult EditArtist(string id)
    {
        var artist = _artistRepository.GetById(id);
        if (artist is null) return NotFound();

        var model = new Models.FormModels.ArtistEditModel { Id = artist.Id, Name = artist.Name };
        return View("Edit", model);
    }

    [HttpPost("artists/edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditArtistPost(string id)
    {
        var artist = _artistRepository.GetById(id, includeDeleted: true);
        if (artist is null) return NotFound();

        var ok = await TryUpdateModelAsync<Models.Artist>(artist, "", a => a.Name);
        if (!ok)
        {
            var vm = new Models.FormModels.ArtistEditModel { Id = artist.Id, Name = artist.Name };
            return View("Edit", vm);
        }

        _artistRepository.Update(artist);
        return RedirectToAction(nameof(Artists));
    }

    [HttpGet("artists/delete/{id}")]
    public IActionResult DeleteArtist(string id)
    {
        var artist = _artistRepository.GetById(id);
        if (artist is null) return NotFound();
        return View("Delete", artist);
    }

    [HttpPost("artists/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteArtistConfirmed(string id)
    {
        _artistRepository.SoftDelete(id);
        return RedirectToAction(nameof(Artists));
    }

    [HttpGet("artists/search")]
    public IActionResult SearchArtists(string q)
    {
        q = q?.Trim() ?? string.Empty;
        var all = _artistRepository.GetAll();
        if (string.IsNullOrWhiteSpace(q))
        {
            var top = all.Take(20).Select(a => new { a.Id, a.Name, SpotifyUrl = a.ExternalUrls?.Spotify }).ToList();
            return Json(top);
        }

        var results = all.Where(a => a.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .Select(a => new { a.Id, a.Name, SpotifyUrl = a.ExternalUrls?.Spotify })
            .ToList();

        return Json(results);
    }

    [HttpGet("playlists")]
    public IActionResult Playlists()
    {
        var playlists = _playlistRepository.GetAll();
        return View(playlists);
    }
}