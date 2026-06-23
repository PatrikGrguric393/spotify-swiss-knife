using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

// Data access for tracks: synchronous CRUD over the EF Core context, eager-loading the
// album, artists, and images each track is shown with. Scoped per request.
public class TrackRepository
{
    private readonly SpotifyDbContext _context;

    public TrackRepository(SpotifyDbContext context)
    {
        _context = context;
    }

    public List<Track> GetAll()
    {
        return _context.Tracks
            .Include(track => track.Album)
            .Include(track => track.Artists)
            .AsTracking()
            .ToList();
    }

    public Track? GetById(string id)
    {
        return _context.Tracks
            .Include(track => track.Album)
            .Include(track => track.Artists)
            .FirstOrDefault(track => track.Id == id);
    }

    public void Add(Track track)
    {
        _context.Tracks.Add(track);
        _context.SaveChanges();
    }

    public void Update(Track track)
    {
        _context.Tracks.Update(track);
        _context.SaveChanges();
    }

    // Case-insensitive duplicate-name check, optionally excluding one track (used when editing
    // so a track doesn't clash with itself). Mirrors AlbumRepository/ArtistRepository/PlaylistRepository.
    public bool ExistsByName(string name, string? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var normalized = name.Trim().ToLowerInvariant();
        var query = _context.Tracks.AsQueryable();
        if (!string.IsNullOrWhiteSpace(excludeId))
            query = query.Where(track => track.Id != excludeId);

        return query.Any(track => track.Name.Trim().ToLower() == normalized);
    }

    public void Delete(string id)
    {
        var track = _context.Tracks.FirstOrDefault(existing => existing.Id == id);
        if (track is null) return;

        if (track.AlbumId is not null)
        {
            var album = _context.Albums.Find(track.AlbumId);
            if (album is not null)
                album.TotalTracks = _context.Tracks.Count(t => t.AlbumId == track.AlbumId && t.Id != id);
        }

        // PlaylistTrackEntry.Track uses Restrict, so remove playlist links before deleting the track
        var entries = _context.PlaylistTrackEntries.Where(entry => entry.TrackId == id);
        _context.PlaylistTrackEntries.RemoveRange(entries);
        _context.Tracks.Remove(track);
        _context.SaveChanges();
    }
}
