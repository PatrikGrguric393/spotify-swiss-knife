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

    [HttpGet("songs/create")]
    public IActionResult CreateSong()
    {
        PopulateArtistOptions([]);
        return View("CreateSong", new Models.FormModels.TrackCreateModel());
    }

    [HttpPost("songs/create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreateSongPost([FromForm] Models.FormModels.TrackCreateModel model)
    {
        if (!TryParseDuration(model.Duration, out var durationMs))
        {
            ModelState.AddModelError(nameof(model.Duration), "Enter duration as seconds (e.g. 213) or minutes:seconds (e.g. 3:33), up to 1 hour.");
        }

        if (!ModelState.IsValid)
        {
            PopulateArtistOptions(model.ArtistIds);
            return View("CreateSong", model);
        }

        var selectedArtists = GetSelectedArtists(model.ArtistIds);

        var track = new Models.Track
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = model.Name.Trim(),
            TrackNumber = model.TrackNumber,
            DiscNumber = model.DiscNumber,
            DurationMs = durationMs,
            IsLocal = model.IsLocal,
            ExternalUrls = new Models.ExternalUrls(),
            Artists = selectedArtists
        };

        _trackRepository.Add(track);
        return RedirectToAction(nameof(Songs));
    }

    [HttpGet("songs/edit/{id}")]
    public IActionResult EditSong(string id)
    {
        var track = _trackRepository.GetById(id);
        if (track is null)
        {
            return NotFound();
        }

        var artistIds = track.Artists.Select(a => a.Id).ToList();
        PopulateArtistOptions(artistIds);

        var model = new Models.FormModels.TrackEditModel
        {
            Id = track.Id,
            Name = track.Name,
            TrackNumber = track.TrackNumber,
            DiscNumber = track.DiscNumber,
            Duration = FormatDuration(track.DurationMs),
            IsLocal = track.IsLocal,
            ArtistIds = artistIds
        };

        return View("EditSong", model);
    }

    [HttpPost("songs/edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditSongPost(string id, [FromForm] Models.FormModels.TrackEditModel model)
    {
        var track = _trackRepository.GetById(id);
        if (track is null)
        {
            return NotFound();
        }

        if (!TryParseDuration(model.Duration, out var durationMs))
        {
            ModelState.AddModelError(nameof(model.Duration), "Enter duration as seconds (e.g. 213) or minutes:seconds (e.g. 3:33), up to 1 hour.");
        }

        if (!ModelState.IsValid)
        {
            PopulateArtistOptions(model.ArtistIds);
            return View("EditSong", model);
        }

        var selectedArtists = GetSelectedArtists(model.ArtistIds);

        track.Name = model.Name.Trim();
        track.TrackNumber = model.TrackNumber;
        track.DiscNumber = model.DiscNumber;
        track.DurationMs = durationMs;
        track.IsLocal = model.IsLocal;
        track.Artists = selectedArtists;

        _trackRepository.Update(track);
        return RedirectToAction(nameof(Songs));
    }

    [HttpGet("songs/delete/{id}")]
    public IActionResult DeleteSong(string id)
    {
        var track = _trackRepository.GetById(id);
        if (track is null)
        {
            return NotFound();
        }

        return View("DeleteSong", track);
    }

    [HttpPost("songs/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteSongConfirmed(string id)
    {
        _trackRepository.Delete(id);
        return RedirectToAction(nameof(Songs));
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
            Label = model.Label?.Trim(),
            Popularity = model.Popularity,
            ReleaseDate = (model.ReleaseDate ?? string.Empty).Trim(),
            ReleaseDatePrecision = "day",
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

        var trackIds = _trackRepository.GetAll()
            .Where(t => t.AlbumId == id)
            .Select(t => t.Id)
            .ToList();
        if (trackIds.Count == 0)
        {
            var albumTracks = album.TrackList.Count > 0 ? album.TrackList : album.Tracks.Items;
            trackIds = albumTracks.Select(t => t.Id).ToList();
        }
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

        // Use the route id (not the form field) to exclude current album from duplicate check
        if (_albumRepository.ExistsByName(model.Name, id))
        {
            ModelState.AddModelError("Name", $"An album named '{model.Name.Trim()}' already exists.");
            PopulateTrackOptions(model.TrackIds);
            PopulateArtistOptions(model.ArtistIds);
            return View("EditAlbum", model);
        }

        // Load all tracks once to derive both selected and previously linked sets
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

        // Unlink tracks that were removed from this album
        foreach (var oldTrack in allTracks.Where(t => previousTrackIds.Contains(t.Id) && !newTrackIds.Contains(t.Id)))
        {
            oldTrack.AlbumId = null;
            _trackRepository.Update(oldTrack);
        }

        // Link newly selected tracks
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
        return View("CreateArtist");
    }

    [HttpPost("artists/create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreateArtist([FromForm] Models.FormModels.ArtistCreateModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("CreateArtist", model);
        }

        if (!string.IsNullOrEmpty(model.SpotifyUrl))
        {
            if (!Uri.TryCreate(model.SpotifyUrl, UriKind.Absolute, out var tmp) || !tmp.Host.Contains("spotify.com"))
            {
                ModelState.AddModelError("SpotifyUrl", "Spotify URL must be a valid spotify.com link.");
                return View("CreateArtist", model);
            }
        }

        if (_artistRepository.ExistsByName(model.Name))
        {
            var safe = (model.Name ?? string.Empty).Trim();
            ModelState.AddModelError("Name", $"An artist named '{safe}' already exists.");
            return View("CreateArtist", model);
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

        var model = new Models.FormModels.ArtistEditModel
        {
            Id = artist.Id,
            Name = artist.Name ?? string.Empty,
            SpotifyUrl = artist.ExternalUrls?.Spotify ?? string.Empty
        };
        return View("EditArtist", model);
    }

    [HttpPost("artists/edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditArtistPost(string id, [FromForm] Models.FormModels.ArtistEditModel model)
    {
        var artist = _artistRepository.GetById(id, includeDeleted: true);
        if (artist is null) return NotFound();

        if (!ModelState.IsValid)
        {
            return View("EditArtist", model);
        }

        if (!string.IsNullOrEmpty(model.SpotifyUrl))
        {
            if (!Uri.TryCreate(model.SpotifyUrl, UriKind.Absolute, out var tmp) || !tmp.Host.Contains("spotify.com"))
            {
                ModelState.AddModelError("SpotifyUrl", "Spotify URL must be a valid spotify.com link.");
                return View("EditArtist", model);
            }
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
        return View("DeleteArtist", artist);
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
        {
            filtered = filtered.Where(a =>
                DateOnly.TryParse(a.ReleaseDate, out var rd) && rd >= from);
        }

        if (DateOnly.TryParse(dateTo, out var to))
        {
            filtered = filtered.Where(a =>
                DateOnly.TryParse(a.ReleaseDate, out var rd) && rd <= to);
        }

        var results = filtered
            .Take(20)
            .Select(a => new
            {
                a.Id,
                a.Name,
                Artists = string.Join(", ", a.Artists.Select(ar => ar.Name)),
                ReleaseDate = a.ReleaseDate
            })
            .ToList();

        return Json(results);
    }

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
            filtered = filtered.Where(p =>
                p.LastShuffled.HasValue && DateOnly.FromDateTime(p.LastShuffled.Value) >= from);

        if (DateOnly.TryParse(dateTo, out var to))
            filtered = filtered.Where(p =>
                p.LastShuffled.HasValue && DateOnly.FromDateTime(p.LastShuffled.Value) <= to);

        var results = filtered
            .Take(20)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Owner = p.Owner.DisplayName,
                TracksCount = p.Tracks.Total,
                LastShuffled = p.LastShuffled
            })
            .ToList();

        return Json(results);
    }

    [HttpGet("songs/search")]
    public IActionResult SearchSongs(string q, int? durationMin, int? durationMax)
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

        var results = filtered
            .Take(20)
            .Select(t => new
            {
                t.Id,
                t.Name,
                Artists = string.Join(", ", t.Artists.Select(a => a.Name)),
                t.DurationMs,
                t.DiscNumber,
                t.TrackNumber,
                t.IsLocal
            })
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

    [HttpGet("playlists/create")]
    public IActionResult CreatePlaylist()
    {
        PopulateTrackOptions([]);
        return View("CreatePlaylist", new Models.FormModels.PlaylistCreateModel());
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
        var allTracks = _trackRepository.GetAll();
        var trackById = allTracks.Where(t => wantedIds.Contains(t.Id)).ToDictionary(t => t.Id);

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

        var playlist = new Models.Playlist
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = model.Name.Trim(),
            Description = (model.Description ?? string.Empty).Trim(),
            ExternalUrls = new Models.ExternalUrls(),
            Owner = new Models.Owner(),
            SnapshotId = Guid.NewGuid().ToString("N"),
            Tracks = new Models.PlaylistTracksPage
            {
                Items = items,
                Total = items.Count,
                Limit = items.Count,
                Offset = 0
            }
        };

        _playlistRepository.Add(playlist);
        return RedirectToAction(nameof(Playlists));
    }

    [HttpGet("playlists/edit/{id}")]
    public IActionResult EditPlaylist(string id)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null)
        {
            return NotFound();
        }

        var trackIds = playlist.Tracks.Items.Select(item => item.Track.Id).ToList();
        PopulateTrackOptions(trackIds);

        var model = new Models.FormModels.PlaylistEditModel
        {
            Id = playlist.Id,
            Name = playlist.Name,
            Description = playlist.Description,
            TrackIds = trackIds
        };

        return View("EditPlaylist", model);
    }

    [HttpPost("playlists/edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditPlaylistPost(string id, [FromForm] Models.FormModels.PlaylistEditModel model)
    {
        var playlist = _playlistRepository.GetById(id);
        if (playlist is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            PopulateTrackOptions(model.TrackIds);
            return View("EditPlaylist", model);
        }

        var wantedIds = new HashSet<string>(model.TrackIds ?? []);
        var allTracks = _trackRepository.GetAll();
        var trackById = allTracks.Where(track => wantedIds.Contains(track.Id)).ToDictionary(track => track.Id);

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
        if (playlist is null)
        {
            return NotFound();
        }

        return View("DeletePlaylist", playlist);
    }

    [HttpPost("playlists/delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePlaylistConfirmed(string id)
    {
        _playlistRepository.Delete(id);
        return RedirectToAction(nameof(Playlists));
    }

    private void PopulateTrackOptions(IEnumerable<string>? selectedTrackIds)
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

    private void PopulateArtistOptions(IEnumerable<string>? selectedArtistIds)
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
            // Only add error when value is non-empty; empty string is already caught by [Required]
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

    private const int MaxDurationMs = 3600000;

    private static string FormatDuration(int durationMs)
    {
        var totalSeconds = durationMs / 1000;
        return $"{totalSeconds / 60}:{totalSeconds % 60:D2}";
    }

    // Accepts plain seconds ("213") or minutes:seconds ("3:33"); caps at one hour.
    private static bool TryParseDuration(string? input, out int durationMs)
    {
        durationMs = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var text = input.Trim();
        var parts = text.Split(':');
        int totalSeconds;

        if (parts.Length == 1)
        {
            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out totalSeconds))
            {
                return false;
            }
        }
        else if (parts.Length == 2)
        {
            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
                seconds > 59)
            {
                return false;
            }

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
