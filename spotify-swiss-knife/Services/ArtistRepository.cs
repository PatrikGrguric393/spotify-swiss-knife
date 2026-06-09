using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class ArtistRepository
{
    private readonly SpotifyDbContext _context;

    public ArtistRepository(SpotifyDbContext context)
    {
        _context = context;
    }

    public List<Artist> GetAll(bool includeDeleted = false)
    {
        var query = _context.Artists
            .Include(artist => artist.Albums)
            .Include(artist => artist.Tracks)
            .AsTracking();

        if (!includeDeleted)
            query = query.Where(artist => artist.DeletedAt == null);

        return query.ToList();
    }

    public Artist? GetById(string id, bool includeDeleted = false)
    {
        var query = _context.Artists
            .Include(artist => artist.Albums)
            .Include(artist => artist.Tracks)
            .Where(artist => artist.Id == id);

        if (!includeDeleted)
            query = query.Where(artist => artist.DeletedAt == null);

        return query.FirstOrDefault();
    }

    public void SoftDelete(string id)
    {
        var artist = _context.Artists.FirstOrDefault(a => a.Id == id);
        if (artist is null) return;
        artist.DeletedAt = DateTime.UtcNow;
        _context.SaveChanges();
    }

    public void Add(Artist artist)
    {
        _context.Artists.Add(artist);
        _context.SaveChanges();
    }

    public void Update(Artist artist)
    {
        _context.Artists.Update(artist);
        _context.SaveChanges();
    }

    public bool ExistsByName(string name, string? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var normalized = name.Trim().ToLowerInvariant();

        var query = _context.Artists.AsQueryable()
            .Where(a => a.DeletedAt == null && a.Name != null);
        if (!string.IsNullOrWhiteSpace(excludeId))
            query = query.Where(a => a.Id != excludeId);

        return query.Any(a => a.Name.Trim().ToLower() == normalized);
    }
}
