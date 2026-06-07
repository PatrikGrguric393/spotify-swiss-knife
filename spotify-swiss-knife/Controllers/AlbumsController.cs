using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using spotify_swiss_knife.Services;
using System.Globalization;

namespace spotify_swiss_knife.Controllers;

[Route("lib")]
[Authorize(Roles = "Admin,Editor")]
public class AlbumsController : Controller
{
    private static readonly HashSet<string> AllowedAlbumTypes = ["album", "single", "compilation"];

    private readonly AlbumRepository _albumRepository;
    private readonly TrackRepository _trackRepository;
    private readonly ArtistRepository _artistRepository;

    public AlbumsController(AlbumRepository albumRepository, TrackRepository trackRepository, ArtistRepository artistRepository)
    {
        _albumRepository = albumRepository;
        _trackRepository = trackRepository;
        _artistRepository = artistRepository;
    }

    [AllowAnonymous]
    [HttpGet("albums")]
    public IActionResult Albums()
    {
        return View(_albumRepository.GetAll());
    }

    [HttpGet("albums/create")]
    public IActionResult CreateAlbum()
    {
        var model = new Models.FormModels.AlbumCreateModel();
        PopulateTrackOptions(model.TrackIds);
        PopulateArtistOptions(model.ArtistIds);
        return View(model);
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
            Label = model.Label?.Trim(),
            Popularity = model.Popularity,
            ReleaseDate = (model.ReleaseDate ?? string.Empty).Trim(),
            ReleaseDatePrecision = "day",
            ExternalUrls = new Models.ExternalUrls(),
            Artists = selectedArtists
        };

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
        if (album is null) return NotFound();

        var trackIds = _trackRepository.GetAll().Where(t => t.AlbumId == id).Select(t => t.Id).ToList();
        if (trackIds.Count == 0)
        {
            var albumTracks = album.TrackList.Count > 0 ? album.TrackList : album.Tracks.Items;
            trackIds = albumTracks.Select(t => t.Id).ToList();
        }

        var artistIds = album.Artists.Select(a => a.Id).ToList();
        PopulateTrackOptions(trackIds);
        PopulateArtistOptions(artistIds);

        return View(new Models.FormModels.AlbumEditModel
        {
            Id = album.Id,
            Name = ToTitleCase(album.Name),
            AlbumType = NormalizeAlbumType(album.AlbumType),
            Label = album.Label,
            Popularity = album.Popularity,
            ReleaseDate = album.ReleaseDate,
            TrackIds = trackIds,
            ArtistIds = artistIds
        });
    }

    [HttpPost("albums/edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditAlbumPost(string id, [FromForm] Models.FormModels.AlbumEditModel model)
    {
        var album = _albumRepository.GetById(id);
        if (album is null) return NotFound();

        ValidateAlbumTypeAndTrackCount(model);

        if (!ModelState.IsValid)
        {
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("EditAlbum", model);
        }

        // Use route id to exclude current album from duplicate check
        if (_albumRepository.ExistsByName(model.Name, id))
        {
            ModelState.AddModelError("Name", $"An album named '{model.Name.Trim()}' already exists.");
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("EditAlbum", model);
        }

        var allTracks = _trackRepository.GetAll();
        var wantedIds = new HashSet<string>(model.TrackIds ?? []);
        var selectedTracks = allTracks.Where(t => wantedIds.Contains(t.Id)).ToList();

        if (selectedTracks.Count != wantedIds.Count)
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

        var previousTrackIds = new HashSet<string>(allTracks.Where(t => t.AlbumId == id).Select(t => t.Id));
        var newTrackIds = new HashSet<string>(selectedTracks.Select(t => t.Id));

        album.Name = ToTitleCase(model.Name.Trim());
        album.AlbumType = model.AlbumType;
        album.TotalTracks = selectedTracks.Count;
        album.Label = model.Label?.Trim();
        album.Popularity = model.Popularity;
        album.ReleaseDate = (model.ReleaseDate ?? string.Empty).Trim();
        album.ReleaseDatePrecision = "day";
        album.Artists = selectedArtists;

        _albumRepository.Update(album);

        foreach (var oldTrack in allTracks.Where(t => previousTrackIds.Contains(t.Id) && !newTrackIds.Contains(t.Id)))
        {
            oldTrack.AlbumId = null;
            _trackRepository.Update(oldTrack);
        }

        foreach (var track in selectedTracks)
        {
            track.AlbumId = id;
            _trackRepository.Update(track);
        }

        return RedirectToAction(nameof(Albums));
    }

    [HttpGet("albums/delete/{id}")]
    public IActionResult DeleteAlbum(string id)
    {
        var album = _albumRepository.GetById(id);
        if (album is null) return NotFound();
        return View(album);
    }

    [HttpPost("albums/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAlbumConfirmed(string id)
    {
        _albumRepository.Delete(id);
        return RedirectToAction(nameof(Albums));
    }

    [AllowAnonymous]
    [HttpGet("albums/search")]
    public IActionResult SearchAlbums(string q, string? dateFrom, string? dateTo)
    {
        var all = _albumRepository.GetAll();
        IEnumerable<Models.Album> filtered = string.IsNullOrWhiteSpace(q)
            ? all
            : all.Where(a =>
                a.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.Artists.Any(ar => ar.Name.Contains(q, StringComparison.OrdinalIgnoreCase)));

        if (DateOnly.TryParse(dateFrom, out var from))
            filtered = filtered.Where(a => DateOnly.TryParse(a.ReleaseDate, out var rd) && rd >= from);

        if (DateOnly.TryParse(dateTo, out var to))
            filtered = filtered.Where(a => DateOnly.TryParse(a.ReleaseDate, out var rd) && rd <= to);

        return Json(filtered.Take(20).Select(a => new
        {
            a.Id,
            a.Name,
            Artists = string.Join(", ", a.Artists.Select(ar => ar.Name)),
            ReleaseDate = a.ReleaseDate
        }).ToList());
    }

    private void PopulateTrackOptions(IEnumerable<string>? selectedTrackIds)
    {
        var selected = new HashSet<string>(selectedTrackIds ?? []);
        ViewBag.TrackOptions = _trackRepository.GetAll()
            .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(t => new SelectListItem { Value = t.Id, Text = t.Name, Selected = selected.Contains(t.Id) })
            .ToList();
    }

    private void PopulateArtistOptions(IEnumerable<string>? selectedArtistIds)
    {
        var selected = new HashSet<string>(selectedArtistIds ?? []);
        ViewBag.ArtistOptions = _artistRepository.GetAll()
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(a => new SelectListItem { Value = a.Id, Text = a.Name, Selected = selected.Contains(a.Id) })
            .ToList();
    }

    private List<Models.Track> GetSelectedTracks(IEnumerable<string> trackIds)
    {
        var wanted = new HashSet<string>(trackIds ?? []);
        if (wanted.Count == 0) return [];
        return _trackRepository.GetAll().Where(t => wanted.Contains(t.Id)).ToList();
    }

    private List<Models.Artist> GetSelectedArtists(IEnumerable<string> artistIds)
    {
        var wanted = new HashSet<string>(artistIds ?? []);
        if (wanted.Count == 0) return [];
        return _artistRepository.GetAll().Where(a => wanted.Contains(a.Id)).ToList();
    }

    private void ValidateAlbumTypeAndTrackCount(Models.FormModels.AlbumFormModel model)
    {
        model.AlbumType = NormalizeAlbumType(model.AlbumType);
        if (!AllowedAlbumTypes.Contains(model.AlbumType))
        {
            if (!string.IsNullOrEmpty(model.AlbumType))
                ModelState.AddModelError(nameof(model.AlbumType), "Album type must be one of: album, single, compilation.");
            return;
        }

        if (model.TrackIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.TrackIds), "Select at least one track.");
            return;
        }

        if (model.AlbumType == "single" && model.TrackIds.Count != 1)
            ModelState.AddModelError(nameof(model.TrackIds), "Singles must contain exactly one track.");
    }

    private static string NormalizeAlbumType(string? albumType) =>
        (albumType ?? string.Empty).Trim().ToLowerInvariant();

    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        try { return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.Trim().ToLowerInvariant()); }
        catch { return input.Trim(); }
    }
}
