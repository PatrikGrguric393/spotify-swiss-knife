using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class ArtistMockRepository
{
    private readonly SpotifyDbContext? _context;
    private readonly MockMusicDataSnapshot _snapshot;

    public ArtistMockRepository() : this(MockMusicDataStore.GetSnapshot())
    {
    }

    public ArtistMockRepository(SpotifyDbContext context)
    {
        _context = context;
        _snapshot = MockMusicDataStore.GetSnapshot();
    }

    public ArtistMockRepository(MockMusicDataSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public List<Artist> GetAll()
    {
        if (_context is null)
        {
            return _snapshot.Artists;
        }

        return _context.Artists
            .Include(artist => artist.Albums)
            .Include(artist => artist.Tracks)
            .AsTracking()
            .ToList();
    }

    public Artist? GetById(string id)
    {
        if (_context is null)
        {
            return _snapshot.Artists.FirstOrDefault(artist => artist.Id == id);
        }

        return _context.Artists
            .Include(artist => artist.Albums)
            .Include(artist => artist.Tracks)
            .FirstOrDefault(artist => artist.Id == id);
    }
}