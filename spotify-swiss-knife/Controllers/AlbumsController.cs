using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;
using System.Globalization;

namespace spotify_swiss_knife.Controllers;

public class AlbumsController : LibraryControllerBase
{
    private static readonly HashSet<string> AllowedAlbumTypes = ["album", "single", "compilation"];

    private readonly AlbumRepository _albumRepository;
    private readonly TrackRepository _trackRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly AlbumCoverStorage _coverStorage;
    private readonly SpotifyDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public AlbumsController(
        AlbumRepository albumRepository,
        TrackRepository trackRepository,
        ArtistRepository artistRepository,
        AlbumCoverStorage coverStorage,
        SpotifyDbContext db,
        UserManager<AppUser> userManager)
    {
        _albumRepository = albumRepository;
        _trackRepository = trackRepository;
        _artistRepository = artistRepository;
        _coverStorage = coverStorage;
        _db = db;
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpGet("albums")]
    public IActionResult Index()
    {
        return View(_albumRepository.GetAll());
    }

    [HttpGet("albums/create")]
    public IActionResult Create()
    {
        var model = new Models.FormModels.AlbumCreateForm();
        PopulateTrackOptions(model.TrackIds);
        PopulateArtistOptions(model.ArtistIds);
        return View(model);
    }

    [HttpPost("albums/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost([FromForm] Models.FormModels.AlbumCreateForm model)
    {
        ValidateAlbumTypeAndTrackCount(model);
        ValidateCoverImage(model.CoverImage);

        if (!ModelState.IsValid)
            return InvalidForm(model, "Create");

        if (_albumRepository.ExistsByName(model.Name))
        {
            ModelState.AddModelError("Name", $"An album named '{model.Name.Trim()}' already exists.");
            return InvalidForm(model, "Create");
        }

        var selectedTracks = FilterByIds(_trackRepository.GetAll(), t => t.Id, model.TrackIds);
        if (selectedTracks.Count != model.TrackIds.Count)
        {
            ModelState.AddModelError(nameof(model.TrackIds), "One or more selected tracks are invalid.");
            return InvalidForm(model, "Create");
        }

        var selectedArtists = FilterByIds(_artistRepository.GetAll(), a => a.Id, model.ArtistIds);
        if (selectedArtists.Count != model.ArtistIds.Count)
        {
            ModelState.AddModelError(nameof(model.ArtistIds), "One or more selected artists are invalid.");
            return InvalidForm(model, "Create");
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

        IFormFile? coverImage = null;
        if (model.CoverImage is { Length: > 0 })
        {
            coverImage = model.CoverImage;
            album.CoverImageFileName = await _coverStorage.SaveAsync(coverImage);
            album.CoverImageContentType = AlbumCoverStorage.ResolveContentType(coverImage.FileName);
        }

        _albumRepository.Add(album);

        if (coverImage is not null)
        {
            var userId = _userManager.GetUserId(User)!;
            _db.UserFiles.Add(new UserFile
            {
                UserId = userId,
                OriginalFileName = coverImage.FileName,
                StoredFileName = album.CoverImageFileName!,
                ContentType = album.CoverImageContentType!,
                FileSize = coverImage.Length,
                UploadedAt = DateTime.UtcNow,
                LinkedAlbumId = album.Id
            });
            await _db.SaveChangesAsync();
        }

        foreach (var track in selectedTracks)
        {
            track.AlbumId = album.Id;
            _trackRepository.Update(track);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("albums/edit/{id}")]
    public IActionResult Edit(string id)
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

        return View(new Models.FormModels.AlbumEditForm
        {
            Id = album.Id,
            Name = ToTitleCase(album.Name),
            AlbumType = NormalizeAlbumType(album.AlbumType),
            Label = album.Label,
            Popularity = album.Popularity,
            ReleaseDate = album.ReleaseDate,
            TrackIds = trackIds,
            ArtistIds = artistIds,
            HasExistingCover = album.HasCover
        });
    }

    [HttpPost("albums/edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(string id, [FromForm] Models.FormModels.AlbumEditForm model)
    {
        var album = _albumRepository.GetById(id);
        if (album is null) return NotFound();

        model.HasExistingCover = album.HasCover;

        ValidateAlbumTypeAndTrackCount(model);
        ValidateCoverImage(model.CoverImage);

        if (!ModelState.IsValid)
            return InvalidForm(model, "Edit");

        if (_albumRepository.ExistsByName(model.Name, id))
        {
            ModelState.AddModelError("Name", $"An album named '{model.Name.Trim()}' already exists.");
            return InvalidForm(model, "Edit");
        }

        var allTracks = _trackRepository.GetAll();
        var selectedTracks = FilterByIds(allTracks, t => t.Id, model.TrackIds);

        if (selectedTracks.Count != model.TrackIds.Count)
        {
            ModelState.AddModelError(nameof(model.TrackIds), "One or more selected tracks are invalid.");
            return InvalidForm(model, "Edit");
        }

        var selectedArtists = FilterByIds(_artistRepository.GetAll(), a => a.Id, model.ArtistIds);
        if (selectedArtists.Count != model.ArtistIds.Count)
        {
            ModelState.AddModelError(nameof(model.ArtistIds), "One or more selected artists are invalid.");
            return InvalidForm(model, "Edit");
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

        if (model.CoverImage is { Length: > 0 })
        {
            var previousCover = album.CoverImageFileName;
            album.CoverImageFileName = await _coverStorage.SaveAsync(model.CoverImage);
            album.CoverImageContentType = AlbumCoverStorage.ResolveContentType(model.CoverImage.FileName);
            _coverStorage.Delete(previousCover);

            var existingCoverFile = await _db.UserFiles.FirstOrDefaultAsync(f => f.LinkedAlbumId == id);
            if (existingCoverFile is not null)
                _db.UserFiles.Remove(existingCoverFile);

            var userId = _userManager.GetUserId(User)!;
            _db.UserFiles.Add(new UserFile
            {
                UserId = userId,
                OriginalFileName = model.CoverImage.FileName,
                StoredFileName = album.CoverImageFileName!,
                ContentType = album.CoverImageContentType!,
                FileSize = model.CoverImage.Length,
                UploadedAt = DateTime.UtcNow,
                LinkedAlbumId = id
            });
        }
        else if (model.RemoveCoverImage && album.HasCover)
        {
            var previousCover = album.CoverImageFileName;
            album.CoverImageFileName = null;
            album.CoverImageContentType = null;
            _coverStorage.Delete(previousCover);

            var existingCoverFile = _db.UserFiles.FirstOrDefault(f => f.LinkedAlbumId == id);
            if (existingCoverFile is not null)
                _db.UserFiles.Remove(existingCoverFile);
        }

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

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("albums/delete/{id}")]
    public IActionResult Delete(string id)
    {
        var album = _albumRepository.GetById(id);
        if (album is null) return NotFound();
        return View(album);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("albums/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(string id)
    {
        var album = _albumRepository.GetById(id);
        var coverFileName = album?.CoverImageFileName;

        var coverFile = _db.UserFiles.FirstOrDefault(f => f.LinkedAlbumId == id);
        if (coverFile is not null)
            _db.UserFiles.Remove(coverFile);

        _albumRepository.Delete(id);
        _coverStorage.Delete(coverFileName);

        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpGet("albums/cover/{id}")]
    public IActionResult AlbumCover(string id)
    {
        var album = _albumRepository.GetById(id);
        if (album is null || string.IsNullOrEmpty(album.CoverImageFileName))
            return NotFound();

        var filePath = _coverStorage.GetPhysicalPath(album.CoverImageFileName);
        if (filePath is null)
            return NotFound();

        var contentType = string.IsNullOrEmpty(album.CoverImageContentType)
            ? AlbumCoverStorage.ResolveContentType(album.CoverImageFileName)
            : album.CoverImageContentType;

        return PhysicalFile(filePath, contentType);
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
            ReleaseDate = a.ReleaseDate,
            a.HasCover
        }).ToList());
    }

    private IActionResult InvalidForm(Models.FormModels.AlbumForm model, string viewName)
    {
        PopulateTrackOptions(model.TrackIds);
        PopulateArtistOptions(model.ArtistIds);
        return View(viewName, model);
    }

    private void PopulateTrackOptions(IEnumerable<string>? selectedIds) =>
        ViewBag.TrackOptions = ToSelectList(_trackRepository.GetAll(), t => t.Id, t => t.Name, selectedIds);

    private void PopulateArtistOptions(IEnumerable<string>? selectedIds) =>
        ViewBag.ArtistOptions = ToSelectList(_artistRepository.GetAll(), a => a.Id, a => a.Name, selectedIds);

    private void ValidateAlbumTypeAndTrackCount(Models.FormModels.AlbumForm model)
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

    private void ValidateCoverImage(IFormFile? cover)
    {
        if (cover is null || cover.Length == 0) return;
        if (!AlbumCoverStorage.IsAllowed(cover))
            ModelState.AddModelError(nameof(Models.FormModels.AlbumForm.CoverImage), "Cover image must be a JPG, PNG, GIF, or WebP file.");
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
