using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

// Data access for playlists. Storage is relational: a playlist's order lives in
// PlaylistTrackEntry rows (each a PlaylistId/TrackId pair with a SortOrder). The rest of the
// app works with the nested `Tracks`/`Items` paged shape instead, so reads project entries
// into that shape (SyncPlaylistWrappers) and writes fold it back into ordered entries
// (SyncTrackEntries). Scoped per request.
public class PlaylistRepository
{
    private readonly SpotifyDbContext _context;

    public PlaylistRepository(SpotifyDbContext context)
    {
        _context = context;
    }

    public List<Playlist> GetAll()
    {
        var playlists = _context.Playlists
            .Include(playlist => playlist.TrackEntries)
                .ThenInclude(entry => entry.Track)
                    .ThenInclude(track => track.Artists)
            .AsTracking()
            .ToList();

        foreach (var playlist in playlists)
            SyncPlaylistWrappers(playlist);

        return playlists;
    }

    public Playlist? GetById(string id)
    {
        var playlist = _context.Playlists
            .Include(playlist => playlist.TrackEntries)
                .ThenInclude(entry => entry.Track)
                    .ThenInclude(track => track.Artists)
            .AsTracking()
            .FirstOrDefault(playlist => playlist.Id == id);

        if (playlist is null) return null;

        SyncPlaylistWrappers(playlist);
        return playlist;
    }

    public void Add(Playlist playlist)
    {
        _context.Playlists.Add(playlist);
        SyncTrackEntries(playlist);
        _context.SaveChanges();
    }

    // Persists a reorder of an existing playlist. When membership is unchanged and every track
    // is distinct, only the SortOrder of moved entries is rewritten with targeted UPDATEs (the
    // fast path). If tracks were added/removed, or duplicates make a track-id mapping ambiguous,
    // it falls back to a full rebuild of the entry rows. Returns the number of rows affected.
    public int Update(Playlist playlist)
    {
        var orderedTrackIds = playlist.Tracks.Items.Select(item => item.Track.Id).ToList();
        if (orderedTrackIds.Count == 0)
            return _context.SaveChanges();

        var hasUniqueTrackIds = orderedTrackIds.Distinct().Count() == orderedTrackIds.Count;
        var existingEntryCount = _context.PlaylistTrackEntries.Count(entry => entry.PlaylistId == playlist.Id);

        if (!hasUniqueTrackIds || existingEntryCount != orderedTrackIds.Count)
        {
            SyncTrackEntries(playlist);
            return _context.SaveChanges();
        }

        var affectedRows = 0;
        for (var index = 0; index < orderedTrackIds.Count; index++)
        {
            var trackId = orderedTrackIds[index];
            affectedRows += _context.PlaylistTrackEntries
                .Where(entry => entry.PlaylistId == playlist.Id && entry.TrackId == trackId && entry.SortOrder != index)
                .ExecuteUpdate(setters => setters.SetProperty(entry => entry.SortOrder, index));
        }

        return affectedRows + _context.SaveChanges();
    }

    // Persists changes to an already-tracked playlist (unlike Add, which inserts a new one),
    // folding its in-memory track list back into ordered entry rows first.
    public void Save(Playlist playlist)
    {
        SyncTrackEntries(playlist);
        _context.SaveChanges();
    }

    // Case-insensitive duplicate-name check, optionally excluding one playlist (used when
    // editing so a playlist doesn't clash with itself). Mirrors AlbumRepository/ArtistRepository.
    public bool ExistsByName(string name, string? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var normalized = name.Trim().ToLowerInvariant();
        var query = _context.Playlists.AsQueryable();
        if (!string.IsNullOrWhiteSpace(excludeId))
            query = query.Where(playlist => playlist.Id != excludeId);

        return query.Any(playlist => playlist.Name.Trim().ToLower() == normalized);
    }

    public void Delete(string id)
    {
        var playlist = _context.Playlists.FirstOrDefault(existing => existing.Id == id);
        if (playlist is null) return;

        // PlaylistTrackEntry.Playlist cascades, so its entries are removed automatically
        _context.Playlists.Remove(playlist);
        _context.SaveChanges();
    }

    // Read side: builds the nested paged `Tracks`/`Items` view from the stored entry rows,
    // ordered by SortOrder, so callers see tracks in playlist order.
    private static void SyncPlaylistWrappers(Playlist playlist)
    {
        var wrappedTracks = playlist.TrackEntries
            .OrderBy(entry => entry.SortOrder)
            .Select(entry => new PlaylistTrack { Track = entry.Track })
            .ToList();

        var page = new PlaylistTracksPage
        {
            Total = wrappedTracks.Count,
            Limit = wrappedTracks.Count,
            Offset = 0,
            Items = wrappedTracks
        };

        // Tracks and Items are aliases over the same backing field, so one assignment sets both.
        playlist.Tracks = page;
    }

    // Write side: folds the in-memory `Tracks` list back into entry rows. If membership is
    // unchanged and tracks are distinct, it updates SortOrder on the existing entries in place;
    // otherwise it clears and rebuilds them so additions, removals, and duplicates are handled.
    private static void SyncTrackEntries(Playlist playlist)
    {
        var tracks = playlist.Tracks.Items;
        var hasUniqueTrackIds = tracks.Select(item => item.Track.Id).Distinct().Count() == tracks.Count;

        if (hasUniqueTrackIds)
        {
            var desiredOrder = tracks
                .Select((item, index) => new { item.Track.Id, SortOrder = index })
                .ToDictionary(entry => entry.Id, entry => entry.SortOrder);

            var existingEntries = playlist.TrackEntries.ToDictionary(entry => entry.TrackId);

            if (existingEntries.Count == desiredOrder.Count && desiredOrder.Keys.All(existingEntries.ContainsKey))
            {
                foreach (var entry in desiredOrder)
                    existingEntries[entry.Key].SortOrder = entry.Value;

                return;
            }
        }

        playlist.TrackEntries.Clear();

        for (var index = 0; index < tracks.Count; index++)
        {
            var track = tracks[index].Track;
            playlist.TrackEntries.Add(new PlaylistTrackEntry
            {
                PlaylistId = playlist.Id,
                TrackId = track.Id,
                SortOrder = index,
                Playlist = playlist,
                Track = track
            });
        }
    }
}
