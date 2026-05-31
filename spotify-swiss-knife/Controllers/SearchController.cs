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
        {
            return Json(Array.Empty<GlobalSearchResult>());
        }

        var results = new List<GlobalSearchResult>();
        results.AddRange(SearchArtists(query));
        results.AddRange(SearchAlbums(query));
        results.AddRange(SearchTracks(query));
        results.AddRange(SearchPlaylists(query));

        return Json(results);
    }

    private List<GlobalSearchResult> SearchArtists(string query)
    {
        return _artistRepository.GetAll()
            .Select(artist => new
            {
                Artist = artist,
                Score = MatchScore(new string?[]
                {
                    artist.Name,
                    artist.ExternalUrls?.Spotify
                }, query)
            })
            .Where(result => result.Score < int.MaxValue)
            .OrderBy(result => result.Score)
            .ThenBy(result => result.Artist.Name)
            .Take(MaxResultsPerEntity)
            .Select(result => new GlobalSearchResult
            {
                EntityType = "Artist",
                Title = result.Artist.Name.Trim(),
                Subtitle = "Artist",
                Url = BuildSelectionUrl("Artists", result.Artist.Id)
            })
            .ToList();
    }

    private List<GlobalSearchResult> SearchAlbums(string query)
    {
        return _albumRepository.GetAll()
            .Select(album => new
            {
                Album = album,
                Score = MatchScore(new string?[]
                {
                    album.Name,
                    album.AlbumType,
                    album.ReleaseDate,
                    album.ReleaseDatePrecision,
                    album.Label,
                    album.Popularity.ToString(),
                    string.Join(" ", album.Artists.Select(artist => artist.Name))
                }, query)
            })
            .Where(result => result.Score < int.MaxValue)
            .OrderBy(result => result.Score)
            .ThenBy(result => result.Album.Name)
            .Take(MaxResultsPerEntity)
            .Select(result => new GlobalSearchResult
            {
                EntityType = "Album",
                Title = result.Album.Name.Trim(),
                Subtitle = BuildAlbumSubtitle(result.Album),
                Url = BuildSelectionUrl("Albums", result.Album.Id)
            })
            .ToList();
    }

    private List<GlobalSearchResult> SearchTracks(string query)
    {
        return _trackRepository.GetAll()
            .Select(track => new
            {
                Track = track,
                Score = MatchScore(new string?[]
                {
                    track.Name,
                    track.TrackNumber.ToString(),
                    track.DiscNumber.ToString(),
                    track.DurationMs.ToString(),
                    track.IsLocal ? "local" : "streaming",
                    track.Album?.Name,
                    string.Join(" ", track.Artists.Select(artist => artist.Name))
                }, query)
            })
            .Where(result => result.Score < int.MaxValue)
            .OrderBy(result => result.Score)
            .ThenBy(result => result.Track.Name)
            .Take(MaxResultsPerEntity)
            .Select(result => new GlobalSearchResult
            {
                EntityType = "Track",
                Title = result.Track.Name.Trim(),
                Subtitle = BuildTrackSubtitle(result.Track),
                Url = BuildSelectionUrl("Songs", result.Track.Id)
            })
            .ToList();
    }

    private List<GlobalSearchResult> SearchPlaylists(string query)
    {
        return _playlistRepository.GetAll()
            .Select(playlist => new
            {
                Playlist = playlist,
                Score = MatchScore(new string?[]
                {
                    playlist.Name,
                    playlist.Description,
                    playlist.Owner?.DisplayName,
                    playlist.Tracks.Total.ToString()
                }, query)
            })
            .Where(result => result.Score < int.MaxValue)
            .OrderBy(result => result.Score)
            .ThenBy(result => result.Playlist.Name)
            .Take(MaxResultsPerEntity)
            .Select(result => new GlobalSearchResult
            {
                EntityType = "Playlist",
                Title = result.Playlist.Name.Trim(),
                Subtitle = BuildPlaylistSubtitle(result.Playlist),
                Url = BuildSelectionUrl("Playlists", result.Playlist.Id)
            })
            .ToList();
    }

    private string BuildSelectionUrl(string action, string id)
    {
        return Url.Action(action, "Library", new { selected = id }) ?? "/lib";
    }

    private static int MatchScore(IEnumerable<string?> values, string query)
    {
        var best = int.MaxValue;

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var score = MatchScore(value.Trim(), query);
            if (score < best)
            {
                best = score;
            }
        }

        return best;
    }

    private static int MatchScore(string value, string query)
    {
        if (value.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (value.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return int.MaxValue;
    }

    private static string BuildAlbumSubtitle(Album album)
    {
        var artists = string.Join(", ", album.Artists.Select(artist => artist.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Take(2));
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(artists))
        {
            details.Add(artists);
        }

        if (!string.IsNullOrWhiteSpace(album.AlbumType))
        {
            details.Add(album.AlbumType);
        }

        if (!string.IsNullOrWhiteSpace(album.ReleaseDate))
        {
            details.Add(album.ReleaseDate);
        }

        return details.Count == 0 ? "Album" : string.Join(" • ", details);
    }

    private static string BuildTrackSubtitle(Track track)
    {
        var artists = string.Join(", ", track.Artists.Select(artist => artist.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Take(2));
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(artists))
        {
            details.Add(artists);
        }

        details.Add(FormatDuration(track.DurationMs));

        if (track.IsLocal)
        {
            details.Add("Local");
        }

        if (!string.IsNullOrWhiteSpace(track.Album?.Name))
        {
            details.Add(track.Album.Name);
        }

        return string.Join(" • ", details);
    }

    private static string BuildPlaylistSubtitle(Playlist playlist)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(playlist.Owner?.DisplayName))
        {
            details.Add(playlist.Owner.DisplayName);
        }

        details.Add($"{playlist.Tracks.Total} tracks");

        if (!string.IsNullOrWhiteSpace(playlist.Description))
        {
            details.Add(playlist.Description.Trim());
        }

        return string.Join(" • ", details);
    }

    private static string FormatDuration(int durationMs)
    {
        var totalSeconds = Math.Max(0, durationMs / 1000);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes}:{seconds:00}";
    }
}