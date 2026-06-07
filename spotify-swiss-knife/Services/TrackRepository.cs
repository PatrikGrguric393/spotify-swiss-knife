using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

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
            .Include(track => track.Images)
            .AsTracking()
            .ToList();
    }

    public Track? GetById(string id)
    {
        return _context.Tracks
            .Include(track => track.Album)
            .Include(track => track.Artists)
            .Include(track => track.Images)
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
