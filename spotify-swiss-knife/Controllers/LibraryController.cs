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

    [HttpGet("playlists")]
    public IActionResult Playlists()
    {
        var playlists = _playlistRepository.GetAll();
        return View(playlists);
    }
}