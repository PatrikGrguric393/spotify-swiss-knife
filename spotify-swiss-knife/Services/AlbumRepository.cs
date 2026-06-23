using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

// Data access for albums: synchronous CRUD over the EF Core context. Read methods eager-load
// artists, images, and the track list, then project the tracks into the paged `Tracks` shape
// the views and API expect (the relational TrackList is the stored form). Scoped per request.
public class AlbumRepository
{
    private readonly SpotifyDbContext _context;

    public AlbumRepository(SpotifyDbContext context)
    {
        _context = context;
    }

    public List<Album> GetAll()
    {
        var albums = _context.Albums
            .Include(album => album.Artists)
            .Include(album => album.Images)
            .Include(album => album.TrackList).ThenInclude(track => track.Artists)
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
        var album = _context.Albums
            .Include(album => album.Artists)
            .Include(album => album.Images)
            .Include(album => album.TrackList).ThenInclude(track => track.Artists)
            .AsTracking()
            .FirstOrDefault(album => album.Id == id);

        if (album is null) return null;

        album.Tracks = new AlbumTracksPage
        {
            Total = album.TrackList.Count,
            Limit = album.TrackList.Count,
            Offset = 0,
            Items = album.TrackList.ToList()
        };

        return album;
    }

    public void Add(Album album)
    {
        _context.Albums.Add(album);
        _context.SaveChanges();
    }

    public void Update(Album album)
    {
        _context.Albums.Update(album);
        _context.SaveChanges();
    }

    public void Delete(string id)
    {
        var album = _context.Albums.FirstOrDefault(existing => existing.Id == id);
        if (album is null) return;

        _context.Albums.Remove(album);
        _context.SaveChanges();
    }

    public bool ExistsByName(string name, string? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var normalized = name.Trim().ToLowerInvariant();
        var query = _context.Albums.AsQueryable().Where(album => album.Name != null);
        if (!string.IsNullOrWhiteSpace(excludeId))
            query = query.Where(album => album.Id != excludeId);

        return query.Any(album => album.Name!.Trim().ToLower() == normalized);
    }
}
