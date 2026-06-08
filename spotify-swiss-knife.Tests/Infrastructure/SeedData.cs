using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Tests.Infrastructure;

/// <summary>
/// Arrange helpers that insert the minimal valid rows a single test needs,
/// straight through the real DbContext. Each entity gets a unique id so tests
/// never depend on the application's HasData seed or on each other.
/// </summary>
public static class SeedData
{
    public static async Task<Album> CreateAlbumAsync(SpotifyDbContext db, string name = "Seed Album")
    {
        var album = new Album
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            AlbumType = "album",
            ReleaseDate = "2023-01-01",
            ReleaseDatePrecision = "day",
            Popularity = 40,
            ExternalUrls = new ExternalUrls { Spotify = string.Empty }
        };

        db.Albums.Add(album);
        await db.SaveChangesAsync();
        return album;
    }

    public static async Task<Artist> CreateArtistAsync(SpotifyDbContext db, string name = "Seed Artist")
    {
        var artist = new Artist
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            ExternalUrls = new ExternalUrls { Spotify = string.Empty }
        };

        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        return artist;
    }

    public static async Task<Track> CreateTrackAsync(SpotifyDbContext db, string name = "Seed Track", string? albumId = null)
    {
        var track = new Track
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            DurationMs = 180000,
            DiscNumber = 1,
            TrackNumber = 1,
            IsLocal = false,
            AlbumId = albumId,
            ExternalUrls = new ExternalUrls { Spotify = string.Empty }
        };

        db.Tracks.Add(track);
        await db.SaveChangesAsync();
        return track;
    }

    public static async Task<Playlist> CreatePlaylistAsync(SpotifyDbContext db, string name = "Seed Playlist", params string[] trackIds)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = "Seed description",
            SnapshotId = Guid.NewGuid().ToString(),
            ExternalUrls = new ExternalUrls { Spotify = string.Empty },
            Owner = new Owner { DisplayName = "seed" }
        };

        db.Playlists.Add(playlist);
        await db.SaveChangesAsync();

        for (var i = 0; i < trackIds.Length; i++)
        {
            db.PlaylistTrackEntries.Add(new PlaylistTrackEntry
            {
                PlaylistId = playlist.Id,
                TrackId = trackIds[i],
                SortOrder = i
            });
        }

        if (trackIds.Length > 0)
            await db.SaveChangesAsync();

        return playlist;
    }
}
