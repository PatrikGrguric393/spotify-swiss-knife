using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using spotify_swiss_knife.Services;
using System.Globalization;

namespace spotify_swiss_knife.Controllers;

[Route("lib")]
public class LibraryController : Controller
{
    private static readonly HashSet<string> AllowedAlbumTypes = ["album", "single", "compilation"];

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

    [HttpGet("albums/create")]
    public IActionResult CreateAlbum()
    {
        var model = new Models.FormModels.AlbumCreateModel();
        PopulateTrackOptions(model.TrackIds);
        PopulateArtistOptions(model.ArtistIds);
        return View("CreateAlbum", model);
    }

    [HttpPost("albums/create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreateAlbumPost([FromForm] Models.FormModels.AlbumCreateModel model)
    {
        ValidateAlbumTypeAndTrackCount(model);

        if (!ModelState.IsValid)
        {
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("CreateAlbum", model);
        }

        if (_albumRepository.ExistsByName(model.Name))
        {
            ModelState.AddModelError("Name", $"An album named '{model.Name.Trim()}' already exists.");
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("CreateAlbum", model);
        }

        var selectedTracks = GetSelectedTracks(model.TrackIds);
        if (selectedTracks.Count != model.TrackIds.Count)
        {
            ModelState.AddModelError(nameof(model.TrackIds), "One or more selected tracks are invalid.");
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("CreateAlbum", model);
        }

        var selectedArtists = GetSelectedArtists(model.ArtistIds);
        if (selectedArtists.Count != model.ArtistIds.Count)
        {
            ModelState.AddModelError(nameof(model.ArtistIds), "One or more selected artists are invalid.");
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("CreateAlbum", model);
        }

        var album = new Models.Album
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = ToTitleCase(model.Name.Trim()),
            AlbumType = model.AlbumType,
            TotalTracks = selectedTracks.Count,
            Label = (model.Label ?? string.Empty).Trim(),
            Popularity = model.Popularity,
            ReleaseDate = (model.ReleaseDate ?? string.Empty).Trim(),
            ReleaseDatePrecision = string.IsNullOrWhiteSpace(model.ReleaseDatePrecision) ? "day" : model.ReleaseDatePrecision.Trim(),
            ExternalUrls = new Models.ExternalUrls()
        };

        album.Artists = selectedArtists;

        _albumRepository.Add(album);

        foreach (var track in selectedTracks)
        {
            track.AlbumId = album.Id;
            _trackRepository.Update(track);
        }
        return RedirectToAction(nameof(Albums));
    }

    [HttpGet("albums/edit/{id}")]
    public IActionResult EditAlbum(string id)
    {
        var album = _albumRepository.GetById(id);
        if (album is null)
        {
            return NotFound();
        }

        var albumTracks = album.TrackList.Any() ? album.TrackList : album.Tracks.Items;
        var trackIds = albumTracks.Select(track => track.Id).ToList();
        var artistIds = album.Artists.Select(artist => artist.Id).ToList();
        PopulateTrackOptions(trackIds);
        PopulateArtistOptions(artistIds);

        var model = new Models.FormModels.AlbumEditModel
        {
            Id = album.Id,
            Name = ToTitleCase(album.Name),
            AlbumType = NormalizeAlbumType(album.AlbumType),
            Label = album.Label,
            Popularity = album.Popularity,
            ReleaseDate = album.ReleaseDate,
            ReleaseDatePrecision = string.IsNullOrWhiteSpace(album.ReleaseDatePrecision) ? "day" : album.ReleaseDatePrecision,
            TrackIds = trackIds,
            ArtistIds = artistIds
        };

        return View("EditAlbum", model);
    }

    [HttpPost("albums/edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditAlbumPost(string id, [FromForm] Models.FormModels.AlbumEditModel model)
    {
        var album = _albumRepository.GetById(id);
        if (album is null)
        {
            return NotFound();
        }

        ValidateAlbumTypeAndTrackCount(model);

        if (!ModelState.IsValid)
        {
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("EditAlbum", model);
        }

        if (_albumRepository.ExistsByName(model.Name, model.Id))
        {
            ModelState.AddModelError("Name", $"An album named '{model.Name.Trim()}' already exists.");
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("EditAlbum", model);
        }

        var selectedTracks = GetSelectedTracks(model.TrackIds);
        if (selectedTracks.Count != model.TrackIds.Count)
        {
            ModelState.AddModelError(nameof(model.TrackIds), "One or more selected tracks are invalid.");
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("EditAlbum", model);
        }

        var selectedArtists = GetSelectedArtists(model.ArtistIds);
        if (selectedArtists.Count != model.ArtistIds.Count)
        {
            ModelState.AddModelError(nameof(model.ArtistIds), "One or more selected artists are invalid.");
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("EditAlbum", model);
        }

        album.Name = ToTitleCase(model.Name.Trim());
        album.AlbumType = model.AlbumType;
        album.TotalTracks = selectedTracks.Count;
        album.Label = (model.Label ?? string.Empty).Trim();
        album.Popularity = model.Popularity;
        album.ReleaseDate = (model.ReleaseDate ?? string.Empty).Trim();
        album.ReleaseDatePrecision = string.IsNullOrWhiteSpace(model.ReleaseDatePrecision) ? "day" : model.ReleaseDatePrecision.Trim();
        album.Artists = selectedArtists;

        _albumRepository.Update(album);

        foreach (var track in selectedTracks)
        {
            track.AlbumId = album.Id;
            _trackRepository.Update(track);
        }
        return RedirectToAction(nameof(Albums));
    }

    [HttpGet("albums/delete/{id}")]
    public IActionResult DeleteAlbum(string id)
    {
        var album = _albumRepository.GetById(id);
        if (album is null)
        {
            return NotFound();
        }

        return View("DeleteAlbum", album);
    }

    [HttpPost("albums/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAlbumConfirmed(string id)
    {
        _albumRepository.Delete(id);
        return RedirectToAction(nameof(Albums));
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

        // Duplicate name protection
        if (_artistRepository.ExistsByName(model.Name))
        {
            var safe = (model.Name ?? string.Empty).Trim();
            ModelState.AddModelError("Name", $"An artist named '{safe}' already exists.");
            return View("Create", model);
        }

        var artist = new Models.Artist
        {
            Id = Guid.NewGuid().ToString(),
            Name = model.Name,
            ExternalUrls = new Models.ExternalUrls { Spotify = (model.SpotifyUrl ?? string.Empty).Trim() }
        };

        _artistRepository.Add(artist);
        return RedirectToAction(nameof(Artists));
    }

    [HttpGet("artists/edit/{id}")]
    public IActionResult EditArtist(string id)
    {
        var artist = _artistRepository.GetById(id);
        if (artist is null) return NotFound();

        var model = new Models.FormModels.ArtistEditModel { Id = artist.Id, Name = artist.Name ?? string.Empty, SpotifyUrl = artist.ExternalUrls?.Spotify ?? string.Empty };
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
            var vm = new Models.FormModels.ArtistEditModel { Id = artist.Id, Name = artist.Name ?? string.Empty };
            return View("Edit", vm);
        }
        // Bind Spotify URL from form and validate
        var spotifyVal = (Request.Form["SpotifyUrl"].FirstOrDefault() ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(spotifyVal))
        {
            if (!Uri.TryCreate(spotifyVal, UriKind.Absolute, out var tmp) || !tmp.Host.Contains("spotify.com"))
            {
                ModelState.AddModelError("SpotifyUrl", "Spotify URL must be a valid spotify.com link.");
                var vmErr = new Models.FormModels.ArtistEditModel { Id = artist.Id, Name = artist.Name ?? string.Empty, SpotifyUrl = spotifyVal };
                return View("Edit", vmErr);
            }
        }
        artist.ExternalUrls ??= new Models.ExternalUrls();
        artist.ExternalUrls.Spotify = spotifyVal;

        // Duplicate name protection (exclude current)
        if (_artistRepository.ExistsByName(artist.Name, artist.Id))
        {
            var safe = (artist.Name ?? string.Empty).Trim();
            ModelState.AddModelError("Name", $"An artist named '{safe}' already exists.");
            var vm = new Models.FormModels.ArtistEditModel { Id = artist.Id, Name = artist.Name ?? string.Empty, SpotifyUrl = artist.ExternalUrls?.Spotify ?? string.Empty };
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

    [HttpGet("artists/validate-name")]
    public IActionResult ValidateArtistName(string q, string? excludeId)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new { isUnique = false });
        var exists = _artistRepository.ExistsByName(q, excludeId);
        return Json(new { isUnique = !exists });
    }

    [HttpGet("playlists")]
    public IActionResult Playlists()
    {
        var playlists = _playlistRepository.GetAll();
        return View(playlists);
    }

    private void PopulateTrackOptions(IEnumerable<string> selectedTrackIds)
    {
        var selected = new HashSet<string>(selectedTrackIds ?? []);
        var trackOptions = _trackRepository.GetAll()
            .OrderBy(track => track.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(track => new SelectListItem
            {
                Value = track.Id,
                Text = track.Name,
                Selected = selected.Contains(track.Id)
            })
            .ToList();

        ViewBag.TrackOptions = trackOptions;
    }

    private void PopulateArtistOptions(IEnumerable<string> selectedArtistIds)
    {
        var selected = new HashSet<string>(selectedArtistIds ?? []);
        var artistOptions = _artistRepository.GetAll()
            .OrderBy(artist => artist.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(artist => new SelectListItem
            {
                Value = artist.Id,
                Text = artist.Name,
                Selected = selected.Contains(artist.Id)
            })
            .ToList();

        ViewBag.ArtistOptions = artistOptions;
    }

    private void ValidateAlbumTypeAndTrackCount(Models.FormModels.AlbumFormModel model)
    {
        model.AlbumType = NormalizeAlbumType(model.AlbumType);
        if (!AllowedAlbumTypes.Contains(model.AlbumType))
        {
            ModelState.AddModelError(nameof(model.AlbumType), "Album type must be one of: album, single, compilation.");
            return;
        }

        if (model.TrackIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.TrackIds), "Select at least one track.");
            return;
        }

        if (model.AlbumType == "single" && model.TrackIds.Count != 1)
        {
            ModelState.AddModelError(nameof(model.TrackIds), "Singles must contain exactly one track.");
        }
    }

    private static string NormalizeAlbumType(string? albumType)
    {
        return (albumType ?? string.Empty).Trim().ToLowerInvariant();
    }

    private List<Models.Track> GetSelectedTracks(IEnumerable<string> trackIds)
    {
        var wanted = new HashSet<string>(trackIds ?? []);
        if (wanted.Count == 0)
        {
            return [];
        }

        return _trackRepository.GetAll()
            .Where(track => wanted.Contains(track.Id))
            .ToList();
    }

    private List<Models.Artist> GetSelectedArtists(IEnumerable<string> artistIds)
    {
        var wanted = new HashSet<string>(artistIds ?? []);
        if (wanted.Count == 0)
        {
            return [];
        }

        return _artistRepository.GetAll()
            .Where(artist => wanted.Contains(artist.Id))
            .ToList();
    }

    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var text = input.Trim();
        try
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
        }
        catch
        {
            return text;
        }
    }
}