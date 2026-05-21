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
            .Include(track => track.Artists)
            .Include(track => track.Images)
            .FirstOrDefault(track => track.Id == id);
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
}