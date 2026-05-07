using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class PlaylistMockRepository
{
    private readonly SpotifyDbContext? _context;
    private readonly MockMusicDataSnapshot _snapshot;

    public PlaylistMockRepository() : this(MockMusicDataStore.GetSnapshot())
    {
    }

    public PlaylistMockRepository(SpotifyDbContext context)
    {
        _context = context;
        _snapshot = MockMusicDataStore.GetSnapshot();
    }

    public PlaylistMockRepository(MockMusicDataSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public List<Playlist> GetAll()
    {
        if (_context is null)
        {
            return _snapshot.Playlists;
        }

        var playlists = _context.Playlists
            .Include(playlist => playlist.TrackEntries)
                .ThenInclude(entry => entry.Track)
                    .ThenInclude(track => track.Artists)
            .Include(playlist => playlist.TrackEntries)
                .ThenInclude(entry => entry.Track)
                    .ThenInclude(track => track.Images)
            .AsTracking()
            .ToList();

        foreach (var playlist in playlists)
        {
            SyncPlaylistWrappers(playlist);
        }

        return playlists;
    }

    public Playlist? GetById(string id)
    {
        if (_context is null)
        {
            return _snapshot.Playlists.FirstOrDefault(playlist => playlist.Id == id);
        }

        return GetAll().FirstOrDefault(playlist => playlist.Id == id);
    }

    public int Update(Playlist playlist)
    {
        if (_context is null)
        {
            return 0;
        }

        var orderedTrackIds = playlist.Tracks.Items.Select(item => item.Track.Id).ToList();
        if (orderedTrackIds.Count == 0)
        {
            return _context.SaveChanges();
        }

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

    private static void SyncPlaylistWrappers(Playlist playlist)
    {
        var wrappedTracks = playlist.TrackEntries
            .OrderBy(entry => entry.SortOrder)
            .Select(entry => new PlaylistTrack
            {
                Track = entry.Track
            })
            .ToList();

        var page = new PlaylistTracksPage
        {
            Total = wrappedTracks.Count,
            Limit = wrappedTracks.Count,
            Offset = 0,
            Items = wrappedTracks
        };

        playlist.Tracks = page;
        playlist.Items = page;
    }

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
                {
                    existingEntries[entry.Key].SortOrder = entry.Value;
                }

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