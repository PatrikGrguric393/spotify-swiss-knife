using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class AlbumMockRepository
{
    private readonly SpotifyDbContext? _context;
    private readonly MockMusicDataSnapshot _snapshot;

    public AlbumMockRepository() : this(MockMusicDataStore.GetSnapshot())
    {
    }

    public AlbumMockRepository(SpotifyDbContext context)
    {
        _context = context;
        _snapshot = MockMusicDataStore.GetSnapshot();
    }

    public AlbumMockRepository(MockMusicDataSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public List<Album> GetAll()
    {
        if (_context is null)
        {
            return _snapshot.Albums;
        }

        var albums = _context.Albums
            .Include(album => album.Artists)
            .Include(album => album.Images)
            .AsTracking()
            .ToList();

        foreach (var album in albums)
        {
            var tracks = _context.Tracks
                .Where(track => track.AlbumId == album.Id)
                .Include(track => track.Artists)
                .Include(track => track.Images)
                .AsTracking()
                .ToList();

            album.TrackList = tracks;
            album.Tracks = new AlbumTracksPage
            {
                Total = tracks.Count,
                Limit = tracks.Count,
                Offset = 0,
                Items = tracks
            };
        }

        return albums;
    }

    public Album? GetById(string id)
    {
        if (_context is null)
        {
            return _snapshot.Albums.FirstOrDefault(album => album.Id == id);
        }

        return GetAll().FirstOrDefault(album => album.Id == id);
    }
}