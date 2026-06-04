using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class ArtistRepository
{
    private readonly SpotifyDbContext? _context;
    private readonly MusicDataSnapshot _snapshot;

    public ArtistRepository() : this(MusicDataStore.GetSnapshot())
    {
    }

    public ArtistRepository(SpotifyDbContext context)
    {
        _context = context;
        _snapshot = MusicDataStore.GetSnapshot();
    }

    public ArtistRepository(MusicDataSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public List<Artist> GetAll(bool includeDeleted = false)
    {
        if (_context is null)
        {
            return includeDeleted
                ? _snapshot.Artists
                : _snapshot.Artists.Where(artist => artist.DeletedAt == null).ToList();
        }

        var query = _context.Artists
            .Include(artist => artist.Albums)
            .Include(artist => artist.Tracks)
            .AsTracking();

        if (!includeDeleted)
        {
            query = query.Where(artist => artist.DeletedAt == null);
        }

        return query.ToList();
    }

    public Artist? GetById(string id, bool includeDeleted = false)
    {
        if (_context is null)
        {
            var artist = _snapshot.Artists.FirstOrDefault(artist => artist.Id == id);
            if (artist is null) return null;
            return includeDeleted || artist.DeletedAt == null ? artist : null;
        }

        var query = _context.Artists
            .Include(artist => artist.Albums)
            .Include(artist => artist.Tracks)
            .Where(artist => artist.Id == id);

        if (!includeDeleted)
        {
            query = query.Where(artist => artist.DeletedAt == null);
        }

        return query.FirstOrDefault();
    }

    public void SoftDelete(string id)
    {
        if (_context is null)
        {
            var a = _snapshot.Artists.FirstOrDefault(artist => artist.Id == id);
            if (a is not null) a.DeletedAt = DateTime.UtcNow;
            return;
        }

        var artist = _context.Artists.FirstOrDefault(a => a.Id == id);
        if (artist is null) return;
        artist.DeletedAt = DateTime.UtcNow;
        _context.SaveChanges();
    }

    public void Restore(string id)
    {
        if (_context is null)
        {
            var a = _snapshot.Artists.FirstOrDefault(artist => artist.Id == id);
            if (a is not null) a.DeletedAt = null;
            return;
        }

        var artist = _context.Artists.FirstOrDefault(a => a.Id == id);
        if (artist is null) return;
        artist.DeletedAt = null;
        _context.SaveChanges();
    }

    public void Add(Artist artist)
    {
        if (_context is null)
        {
            _snapshot.Artists.Add(artist);
            return;
        }

        _context.Artists.Add(artist);
        _context.SaveChanges();
    }

    public void Update(Artist artist)
    {
        if (_context is null)
        {
            var idx = _snapshot.Artists.FindIndex(a => a.Id == artist.Id);
            if (idx >= 0) _snapshot.Artists[idx] = artist;
            return;
        }

        _context.Artists.Update(artist);
        _context.SaveChanges();
    }

    public bool ExistsByName(string name, string? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var normalized = name.Trim().ToLowerInvariant();

        if (_context is null)
        {
            return _snapshot.Artists.Any(a => !string.IsNullOrWhiteSpace(a.Name)
                && a.Name.Trim().Equals(normalized, StringComparison.OrdinalIgnoreCase)
                && (excludeId == null || a.Id != excludeId)
                && a.DeletedAt == null);
        }

        var query = _context.Artists.AsQueryable();
        query = query.Where(a => a.DeletedAt == null && a.Name != null);
        if (!string.IsNullOrEmpty(excludeId))
        {
            query = query.Where(a => a.Id != excludeId);
        }

        return query.Any(a => a.Name.Trim().ToLower() == normalized);
    }
}