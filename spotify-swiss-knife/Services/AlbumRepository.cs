using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class AlbumRepository
{
    private readonly SpotifyDbContext? _context;
    private readonly MusicDataSnapshot _snapshot;

    public AlbumRepository() : this(MusicDataStore.GetSnapshot())
    {
    }

    public AlbumRepository(SpotifyDbContext context)
    {
        _context = context;
        _snapshot = MusicDataStore.GetSnapshot();
    }

    public AlbumRepository(MusicDataSnapshot snapshot)
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
            .Include(album => album.TrackList).ThenInclude(track => track.Artists)
            .Include(album => album.TrackList).ThenInclude(track => track.Images)
            .AsTracking()
            .ToList();

        foreach (var album in albums)
        {
            album.Tracks = new AlbumTracksPage
            {
                Total = album.TrackList.Count,
                Limit = album.TrackList.Count,
                Offset = 0,
                Items = album.TrackList.ToList()
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

    public void Add(Album album)
    {
        if (_context is null)
        {
            _snapshot.Albums.Add(album);
            return;
        }

        _context.Albums.Add(album);
        _context.SaveChanges();
    }

    public void Update(Album album)
    {
        if (_context is null)
        {
            var index = _snapshot.Albums.FindIndex(existing => existing.Id == album.Id);
            if (index >= 0)
            {
                _snapshot.Albums[index] = album;
            }

            return;
        }

        _context.Albums.Update(album);
        _context.SaveChanges();
    }

    public void Delete(string id)
    {
        if (_context is null)
        {
            _snapshot.Albums.RemoveAll(album => album.Id == id);
            return;
        }

        var album = _context.Albums.FirstOrDefault(existing => existing.Id == id);
        if (album is null)
        {
            return;
        }

        _context.Albums.Remove(album);
        _context.SaveChanges();
    }

    public bool ExistsByName(string name, string? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = name.Trim().ToLowerInvariant();

        if (_context is null)
        {
            return _snapshot.Albums.Any(album =>
                !string.IsNullOrWhiteSpace(album.Name) &&
                album.Name.Trim().Equals(normalized, StringComparison.OrdinalIgnoreCase) &&
                (excludeId == null || album.Id != excludeId));
        }

        var query = _context.Albums.AsQueryable().Where(album => album.Name != null);
        if (!string.IsNullOrWhiteSpace(excludeId))
        {
            query = query.Where(album => album.Id != excludeId);
        }

        return query.Any(album => album.Name!.Trim().ToLower() == normalized);
    }
}
