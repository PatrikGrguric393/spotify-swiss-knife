using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using spotify_swiss_knife.Services;
using System.Globalization;

namespace spotify_swiss_knife.Controllers;

[Route("lib")]
[Authorize(Roles = "Admin,Editor")]
public class TracksController : Controller
{
    private const int MaxDurationMs = 3600000;

    private readonly TrackRepository _trackRepository;
    private readonly ArtistRepository _artistRepository;

    public TracksController(TrackRepository trackRepository, ArtistRepository artistRepository)
    {
        _trackRepository = trackRepository;
        _artistRepository = artistRepository;
    }

    [AllowAnonymous]
    [HttpGet("tracks")]
    public IActionResult Index()
    {
        var tracks = _trackRepository.GetAll();
        return View(tracks);
    }

    [HttpGet("tracks/create")]
    public IActionResult Create()
    {
        PopulateArtistOptions([]);
        return View(new Models.FormModels.TrackCreateForm());
    }

    [HttpPost("tracks/create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePost([FromForm] Models.FormModels.TrackCreateForm model)
    {
        if (!TryParseDuration(model.Duration, out var durationMs))
            ModelState.AddModelError(nameof(model.Duration), "Enter duration as seconds (e.g. 213) or minutes:seconds (e.g. 3:33), up to 1 hour.");

        if (!ModelState.IsValid)
        {
            PopulateArtistOptions(model.ArtistIds);
            return View("Create", model);
        }

        var track = new Models.Track
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = model.Name.Trim(),
            TrackNumber = model.TrackNumber,
            DiscNumber = model.DiscNumber,
            DurationMs = durationMs,
            IsLocal = model.IsLocal,
            ExternalUrls = new Models.ExternalUrls(),
            Artists = GetSelectedArtists(model.ArtistIds)
        };

        _trackRepository.Add(track);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("tracks/edit/{id}")]
    public IActionResult Edit(string id)
    {
        var track = _trackRepository.GetById(id);
        if (track is null) return NotFound();

        var artistIds = track.Artists.Select(a => a.Id).ToList();
        PopulateArtistOptions(artistIds);

        return View(new Models.FormModels.TrackEditForm
        {
            Id = track.Id,
            Name = track.Name,
            TrackNumber = track.TrackNumber,
            DiscNumber = track.DiscNumber,
            Duration = FormatDuration(track.DurationMs),
            IsLocal = track.IsLocal,
            ArtistIds = artistIds
        });
    }

    [HttpPost("tracks/edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditPost(string id, [FromForm] Models.FormModels.TrackEditForm model)
    {
        var track = _trackRepository.GetById(id);
        if (track is null) return NotFound();

        if (!TryParseDuration(model.Duration, out var durationMs))
            ModelState.AddModelError(nameof(model.Duration), "Enter duration as seconds (e.g. 213) or minutes:seconds (e.g. 3:33), up to 1 hour.");

        if (!ModelState.IsValid)
        {
            PopulateArtistOptions(model.ArtistIds);
            return View("Edit", model);
        }

        track.Name = model.Name.Trim();
        track.TrackNumber = model.TrackNumber;
        track.DiscNumber = model.DiscNumber;
        track.DurationMs = durationMs;
        track.IsLocal = model.IsLocal;
        track.Artists = GetSelectedArtists(model.ArtistIds);

        _trackRepository.Update(track);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("tracks/delete/{id}")]
    public IActionResult Delete(string id)
    {
        var track = _trackRepository.GetById(id);
        if (track is null) return NotFound();
        return View(track);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("tracks/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(string id)
    {
        _trackRepository.Delete(id);
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpGet("tracks/search")]
    public IActionResult SearchTracks(string q, int? durationMin, int? durationMax)
    {
        var all = _trackRepository.GetAll();
        IEnumerable<Models.Track> filtered = string.IsNullOrWhiteSpace(q)
            ? all
            : all.Where(t =>
                t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                t.Artists.Any(a => a.Name.Contains(q, StringComparison.OrdinalIgnoreCase)));

        if (durationMin.HasValue)
            filtered = filtered.Where(t => t.DurationMs >= durationMin.Value * 1000);

        if (durationMax.HasValue)
            filtered = filtered.Where(t => t.DurationMs <= durationMax.Value * 1000);

        return Json(filtered.Take(20).Select(t => new
        {
            t.Id,
            t.Name,
            Artists = string.Join(", ", t.Artists.Select(a => a.Name)),
            t.DurationMs,
            t.DiscNumber,
            t.TrackNumber,
            t.IsLocal
        }).ToList());
    }

    private void PopulateArtistOptions(IEnumerable<string>? selectedArtistIds)
    {
        var selected = new HashSet<string>(selectedArtistIds ?? []);
        ViewBag.ArtistOptions = _artistRepository.GetAll()
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(a => new SelectListItem { Value = a.Id, Text = a.Name, Selected = selected.Contains(a.Id) })
            .ToList();
    }

    private List<Models.Artist> GetSelectedArtists(IEnumerable<string> artistIds)
    {
        var wanted = new HashSet<string>(artistIds ?? []);
        if (wanted.Count == 0) return [];
        return _artistRepository.GetAll().Where(a => wanted.Contains(a.Id)).ToList();
    }

    private static string FormatDuration(int durationMs)
    {
        var totalSeconds = durationMs / 1000;
        return $"{totalSeconds / 60}:{totalSeconds % 60:D2}";
    }

    private static bool TryParseDuration(string? input, out int durationMs)
    {
        durationMs = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var parts = input.Trim().Split(':');
        int totalSeconds;

        if (parts.Length == 1)
        {
            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out totalSeconds))
                return false;
        }
        else if (parts.Length == 2)
        {
            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
                seconds > 59)
                return false;
            totalSeconds = minutes * 60 + seconds;
        }
        else
        {
            return false;
        }

        durationMs = totalSeconds * 1000;
        return durationMs >= 0 && durationMs <= MaxDurationMs;
    }
}
