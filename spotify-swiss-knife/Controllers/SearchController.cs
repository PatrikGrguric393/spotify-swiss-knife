using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[Route("search")]
public class SearchController : Controller
{
    private const int MaxResultsPerEntity = 5;

    private readonly ArtistRepository _artistRepository;
    private readonly AlbumRepository _albumRepository;
    private readonly TrackRepository _trackRepository;
    private readonly PlaylistRepository _playlistRepository;

    public SearchController(
        ArtistRepository artistRepository,
        AlbumRepository albumRepository,
        TrackRepository trackRepository,
        PlaylistRepository playlistRepository)
    {
        _artistRepository = artistRepository;
        _albumRepository = albumRepository;
        _trackRepository = trackRepository;
        _playlistRepository = playlistRepository;
    }

    [HttpGet("")]
    public IActionResult Index(string? q)
    {
        var query = (q ?? string.Empty).Trim();
        if (query.Length < 2)
            return Json(Array.Empty<GlobalSearchResult>());

        var results = new List<GlobalSearchResult>();
        results.AddRange(SearchPageResults(query));
        results.AddRange(SearchEntities(
            _artistRepository.GetAll(), query,
            a => [a.Name, a.ExternalUrls?.Spotify],
            a => a.Name,
            a => new GlobalSearchResult { EntityType = "Artist", Title = a.Name.Trim(), Subtitle = "Artist", Url = BuildSelectionUrl("Artists", a.Id) }
        ));
        results.AddRange(SearchEntities(
            _albumRepository.GetAll(), query,
            a => [a.Name, a.AlbumType, a.ReleaseDate, a.ReleaseDatePrecision, a.Label, a.Popularity.ToString(), string.Join(" ", a.Artists.Select(ar => ar.Name))],
            a => a.Name,
            a => new GlobalSearchResult { EntityType = "Album", Title = a.Name.Trim(), Subtitle = BuildAlbumSubtitle(a), Url = BuildSelectionUrl("Albums", a.Id) }
        ));
        results.AddRange(SearchEntities(
            _trackRepository.GetAll(), query,
            t => [t.Name, t.TrackNumber.ToString(), t.DiscNumber.ToString(), t.DurationMs.ToString(), t.IsLocal ? "local" : "streaming", t.Album?.Name, string.Join(" ", t.Artists.Select(a => a.Name))],
            t => t.Name,
            t => new GlobalSearchResult { EntityType = "Track", Title = t.Name.Trim(), Subtitle = BuildTrackSubtitle(t), Url = BuildSelectionUrl("Tracks", t.Id) }
        ));
        results.AddRange(SearchEntities(
            _playlistRepository.GetAll(), query,
            p => [p.Name, p.Description, p.Owner?.DisplayName, p.Tracks.Total.ToString()],
            p => p.Name,
            p => new GlobalSearchResult { EntityType = "Playlist", Title = p.Name.Trim(), Subtitle = BuildPlaylistSubtitle(p), Url = BuildSelectionUrl("Playlists", p.Id) }
        ));

        return Json(results);
    }

    private List<GlobalSearchResult> SearchEntities<T>(
        IEnumerable<T> source,
        string query,
        Func<T, string?[]> fields,
        Func<T, string> tieBreaker,
        Func<T, GlobalSearchResult> project)
    {
        return source
            .Select(item => new { Item = item, Score = MatchScore(fields(item), query) })
            .Where(r => r.Score < int.MaxValue)
            .OrderBy(r => r.Score)
            .ThenBy(r => tieBreaker(r.Item))
            .Take(MaxResultsPerEntity)
            .Select(r => project(r.Item))
            .ToList();
    }

    private string BuildSelectionUrl(string section, string id)
    {
        return Url.Action(section, section, new { selected = id }) ?? "/lib";
    }

    private static int MatchScore(IEnumerable<string?> values, string query)
    {
        var best = int.MaxValue;
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            var score = MatchScore(value.Trim(), query);
            if (score < best) best = score;
        }
        return best;
    }

    private static int MatchScore(string value, string query)
    {
        if (value.Equals(query, StringComparison.OrdinalIgnoreCase)) return 0;
        if (value.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 1;
        if (value.Contains(query, StringComparison.OrdinalIgnoreCase)) return 2;
        return int.MaxValue;
    }

    private static string BuildAlbumSubtitle(Album album)
    {
        var details = new List<string>();
        var artists = string.Join(", ", album.Artists.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Take(2));
        if (!string.IsNullOrWhiteSpace(artists)) details.Add(artists);
        if (!string.IsNullOrWhiteSpace(album.AlbumType)) details.Add(album.AlbumType);
        if (!string.IsNullOrWhiteSpace(album.ReleaseDate)) details.Add(album.ReleaseDate);
        return details.Count == 0 ? "Album" : string.Join(" • ", details);
    }

    private static string BuildTrackSubtitle(Track track)
    {
        var details = new List<string>();
        var artists = string.Join(", ", track.Artists.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Take(2));
        if (!string.IsNullOrWhiteSpace(artists)) details.Add(artists);
        details.Add(FormatDuration(track.DurationMs));
        if (track.IsLocal) details.Add("Local");
        if (!string.IsNullOrWhiteSpace(track.Album?.Name)) details.Add(track.Album.Name);
        return string.Join(" • ", details);
    }

    private static string BuildPlaylistSubtitle(Playlist playlist)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(playlist.Owner?.DisplayName)) details.Add(playlist.Owner.DisplayName);
        details.Add($"{playlist.Tracks.Total} tracks");
        if (!string.IsNullOrWhiteSpace(playlist.Description)) details.Add(playlist.Description.Trim());
        return string.Join(" • ", details);
    }

    private static string FormatDuration(int durationMs)
    {
        var totalSeconds = Math.Max(0, durationMs / 1000);
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    private IEnumerable<GlobalSearchResult> SearchPageResults(string query)
    {
        var pages = new (string Title, string Subtitle, string[] Keywords, string? Url)[]
        {
            ("Songs",               "Library",  ["songs", "tracks", "library"],                     Url.Action("Index", "Tracks")),
            ("Albums",              "Library",  ["albums", "library"],                              Url.Action("Index", "Albums")),
            ("Artists",             "Library",  ["artists", "library"],                             Url.Action("Index", "Artists")),
            ("Playlists",           "Library",  ["playlists", "library"],                           Url.Action("Index", "Playlists")),
            ("Files",               "Library",  ["files", "local files", "library"],               Url.Action("Index", "Files")),
            ("Shuffle Playlist",    "Services", ["shuffle", "playlist", "random"],                  Url.Action("Index", "Shuffle")),
            ("Scheduled Shuffles",  "Services", ["schedules", "scheduled", "shuffle", "automation"], Url.Action("Index", "Schedules")),
            ("Bulk Album Save",     "Services", ["bulk", "album", "save", "add albums"],            Url.Action("Index", "BulkAlbumSave")),
            ("Home",                "Home",     ["home", "dashboard"],                              Url.Action("Index", "Home")),
            ("About",               "About",    ["about"],                                          Url.Action("Index", "About")),
            ("Login",               "Account",  ["login", "sign in"],                              Url.Action("Index", "Login")),
        };

        return pages
            .Select(p => new { Page = p, Score = MatchScore(p.Keywords.Concat([p.Title]).Select(k => (string?)k), query) })
            .Where(r => r.Score < int.MaxValue)
            .OrderBy(r => r.Score)
            .ThenBy(r => r.Page.Title)
            .Take(MaxResultsPerEntity)
            .Select(r => new GlobalSearchResult
            {
                EntityType = "Page",
                Title = r.Page.Title,
                Subtitle = r.Page.Subtitle,
                Url = r.Page.Url ?? "/"
            });
    }
}
