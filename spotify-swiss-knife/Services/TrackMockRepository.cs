using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class TrackMockRepository
{
    private readonly SpotifyDbContext? _context;
    private readonly MockMusicDataSnapshot _snapshot;

    public TrackMockRepository() : this(MockMusicDataStore.GetSnapshot())
    {
    }

    public TrackMockRepository(SpotifyDbContext context)
    {
        _context = context;
        _snapshot = MockMusicDataStore.GetSnapshot();
    }

    public TrackMockRepository(MockMusicDataSnapshot snapshot)
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
}