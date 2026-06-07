using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[Route("lib")]
[Authorize(Roles = "Admin,Editor")]
public class PlaylistsController : Controller
{
    private readonly PlaylistRepository _playlistRepository;
    private readonly TrackRepository _trackRepository;

    public PlaylistsController(PlaylistRepository playlistRepository, TrackRepository trackRepository)
    {
        _playlistRepository = playlistRepository;
        _trackRepository = trackRepository;
    }

    [AllowAnonymous]
    [HttpGet("playlists")]
    public IActionResult Playlists()
    {
        return View(_playlistRepository.GetAll());
    }

    [HttpGet("playlists/create")]
    public IActionResult CreatePlaylist()
    {
        PopulateTrackOptions([]);
        return View(new Models.FormModels.PlaylistCreateModel());
    }

    [HttpPost("playlists/create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePlaylistPost([FromForm] Models.FormModels.PlaylistCreateModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateTrackOptions(model.TrackIds);
            return View("CreatePlaylist", model);
        }

        var wantedIds = new HashSet<string>(model.TrackIds ?? []);
        var trackById = _trackRepository.GetAll().Where(t => wantedIds.Contains(t.Id)).ToDictionary(t => t.Id);

        if (trackById.Count != wantedIds.Count)
        {
            ModelState.AddModelError(nameof(model.TrackIds), "One or more selected songs are invalid.");
            PopulateTrackOptions(model.TrackIds);
            return View("CreatePlaylist", model);
        }

        var items = (model.TrackIds ?? [])
            .Where(trackById.ContainsKey)
            .Select(id => new Models.PlaylistTrack { Track = trackById[id] })
            .ToList();

        _playlistRepository.Add(new Models.Playlist
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = model.Name.Trim(),
            Description = (model.Description ?? string.Empty).Trim(),
            ExternalUrls = new Models.ExternalUrls(),
            Owner = new Models.Owner(),
            SnapshotId = Guid.NewGuid().ToString("N"),
            Tracks = new Models.PlaylistTracksPage { Items = items, Total = items.Count, Limit = items.Count, Offset = 0 }
        });

        return RedirectToAction(nameof(Playlists));
    }

    [HttpGet("playlists/edit/{id}")]
    public IActionResult EditPlaylist(string id)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null) return NotFound();

        var trackIds = playlist.Tracks.Items.Select(item => item.Track.Id).ToList();
        PopulateTrackOptions(trackIds);

        return View(new Models.FormModels.PlaylistEditModel
        {
            Id = playlist.Id,
            Name = playlist.Name,
            Description = playlist.Description,
            TrackIds = trackIds
        });
    }

    [HttpPost("playlists/edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditPlaylistPost(string id, [FromForm] Models.FormModels.PlaylistEditModel model)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null) return NotFound();

        if (!ModelState.IsValid)
        {
            PopulateTrackOptions(model.TrackIds);
            return View("EditPlaylist", model);
        }

        var wantedIds = new HashSet<string>(model.TrackIds ?? []);
        var trackById = _trackRepository.GetAll().Where(t => wantedIds.Contains(t.Id)).ToDictionary(t => t.Id);

        if (trackById.Count != wantedIds.Count)
        {
            ModelState.AddModelError(nameof(model.TrackIds), "One or more selected songs are invalid.");
            PopulateTrackOptions(model.TrackIds);
            return View("EditPlaylist", model);
        }

        playlist.Name = model.Name.Trim();
        playlist.Description = (model.Description ?? string.Empty).Trim();

        // Preserve existing order for retained songs, then append newly added ones
        var previousOrder = playlist.Tracks.Items.Select(item => item.Track.Id).ToList();
        var previousSet = new HashSet<string>(previousOrder);
        var orderedIds = previousOrder.Where(wantedIds.Contains).ToList();
        orderedIds.AddRange((model.TrackIds ?? []).Where(trackId => !previousSet.Contains(trackId)));

        var seen = new HashSet<string>();
        var items = orderedIds
            .Where(seen.Add)
            .Select(trackId => new Models.PlaylistTrack { Track = trackById[trackId] })
            .ToList();

        playlist.Tracks = new Models.PlaylistTracksPage
        {
            Items = items,
            Total = items.Count,
            Limit = items.Count,
            Offset = 0
        };

        _playlistRepository.Save(playlist);
        return RedirectToAction(nameof(Playlists));
    }

    [HttpGet("playlists/delete/{id}")]
    public IActionResult DeletePlaylist(string id)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null) return NotFound();
        return View(playlist);
    }

    [HttpPost("playlists/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePlaylistConfirmed(string id)
    {
        _playlistRepository.Delete(id);
        return RedirectToAction(nameof(Playlists));
    }

    [AllowAnonymous]
    [HttpGet("playlists/search")]
    public IActionResult SearchPlaylists(string q, string? dateFrom, string? dateTo)
    {
        var all = _playlistRepository.GetAll();
        IEnumerable<Models.Playlist> filtered = string.IsNullOrWhiteSpace(q)
            ? all
            : all.Where(p =>
                p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (p.Owner.DisplayName ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (p.Description ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Tracks.Total.ToString().Contains(q, StringComparison.OrdinalIgnoreCase));

        if (DateOnly.TryParse(dateFrom, out var from))
            filtered = filtered.Where(p => p.LastShuffled.HasValue && DateOnly.FromDateTime(p.LastShuffled.Value) >= from);

        if (DateOnly.TryParse(dateTo, out var to))
            filtered = filtered.Where(p => p.LastShuffled.HasValue && DateOnly.FromDateTime(p.LastShuffled.Value) <= to);

        return Json(filtered.Take(20).Select(p => new
        {
            p.Id,
            p.Name,
            Owner = p.Owner.DisplayName,
            TracksCount = p.Tracks.Total,
            LastShuffled = p.LastShuffled
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
}
