using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class TrackRepository
{
    private readonly SpotifyDbContext? _context;
    private readonly MusicDataSnapshot _snapshot;

    public TrackRepository() : this(MusicDataStore.GetSnapshot())
    {
    }

    public TrackRepository(SpotifyDbContext context)
    {
        _context = context;
        _snapshot = MusicDataStore.GetSnapshot();
    }

    public TrackRepository(MusicDataSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public List<Track> GetAll()
    {
        if (_context is null)
        {
            return _snapshot.Tracks;
        }

        return _context.Tracks
            .Include(track => track.Album)
            .Include(track => track.Artists)
            .Include(track => track.Images)
            .AsTracking()
            .ToList();
    }

    public Track? GetById(string id)
    {
        if (_context is null)
        {
            return _snapshot.Tracks.FirstOrDefault(track => track.Id == id);
        }

        return _context.Tracks
            .Include(track => track.Album)
            .Include(track => track.Artists)
            .Include(track => track.Images)
            .FirstOrDefault(track => track.Id == id);
    }

    public void Add(Track track)
    {
        if (_context is null)
        {
            _snapshot.Tracks.Add(track);
            return;
        }

        _context.Tracks.Add(track);
        _context.SaveChanges();
    }

    public void Update(Track track)
    {
        if (_context is null)
        {
            var index = _snapshot.Tracks.FindIndex(existing => existing.Id == track.Id);
            if (index >= 0)
            {
                _snapshot.Tracks[index] = track;
            }

            return;
        }

        _context.Tracks.Update(track);
        _context.SaveChanges();
    }

    public void Delete(string id)
    {
        if (_context is null)
        {
            _snapshot.Tracks.RemoveAll(track => track.Id == id);
            return;
        }

        var track = _context.Tracks.FirstOrDefault(existing => existing.Id == id);
        if (track is null)
        {
            return;
        }

        // PlaylistTrackEntry.Track uses Restrict, so remove playlist links before deleting the track
        var entries = _context.PlaylistTrackEntries.Where(entry => entry.TrackId == id);
        _context.PlaylistTrackEntries.RemoveRange(entries);
        _context.Tracks.Remove(track);
        _context.SaveChanges();
    }
}