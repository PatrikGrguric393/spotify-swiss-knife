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

    public void Update(Playlist playlist)
    {
        if (_context is null)
        {
            return;
        }

        SyncTrackEntries(playlist);
        _context.SaveChanges();
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
        playlist.TrackEntries.Clear();

        var tracks = playlist.Tracks.Items;
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