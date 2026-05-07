using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public static class MusicDataStore
{
    private static readonly MusicDataSnapshot Snapshot = CreateSnapshot();

    public static MusicDataSnapshot GetSnapshot() => Snapshot;

    private static MusicDataSnapshot CreateSnapshot()
    {
        var luna = new Artist
        {
            Id = "artist-luna-wave",
            Name = "Luna Wave",
            ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/artist/artist-luna-wave" }
        };

        var neons = new Artist
        {
            Id = "artist-neon-meadow",
            Name = "Neon Meadow",
            ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/artist/artist-neon-meadow" }
        };

        var blackbird = new Artist
        {
            Id = "artist-blackbird-theory",
            Name = "Blackbird Theory",
            ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/artist/artist-blackbird-theory" }
        };

        var ember = new Artist
        {
            Id = "artist-ember-kite",
            Name = "Ember Kite",
            ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/artist/artist-ember-kite" }
        };

        var tracks = new List<Track>
        {
            new()
            {
                Id = "track-midnight-circuit",
                Name = "Midnight Circuit",
                DurationMs = 213000,
                DiscNumber = 1,
                TrackNumber = 1,
                IsLocal = false,
                Artists = [luna],
                Images =
                [
                    new Image { Url = "https://images.example.com/tracks/midnight-circuit-640.jpg", Height = 640, Width = 640 },
                    new Image { Url = "https://images.example.com/tracks/midnight-circuit-300.jpg", Height = 300, Width = 300 }
                ],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/track/track-midnight-circuit" }
            },
            new()
            {
                Id = "track-gravity-bloom",
                Name = "Gravity Bloom",
                DurationMs = 188000,
                DiscNumber = 1,
                TrackNumber = 2,
                IsLocal = false,
                Artists = [luna, neons],
                Images =
                [
                    new Image { Url = "https://images.example.com/tracks/gravity-bloom.jpg", Height = 640, Width = 640 }
                ],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/track/track-gravity-bloom" }
            },
            new()
            {
                Id = "track-river-in-binary",
                Name = "River in Binary",
                DurationMs = 241000,
                DiscNumber = 1,
                TrackNumber = 3,
                IsLocal = false,
                Artists = [blackbird],
                Images =
                [
                    new Image { Url = "https://images.example.com/tracks/river-in-binary.jpg", Height = null, Width = null }
                ],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/track/track-river-in-binary" }
            },
            new()
            {
                Id = "track-static-sunrise",
                Name = "Static Sunrise",
                DurationMs = 201000,
                DiscNumber = 2,
                TrackNumber = 1,
                IsLocal = true,
                Artists = [ember],
                Images =
                [
                    new Image { Url = "https://images.example.com/tracks/static-sunrise.jpg", Height = 512, Width = 512 }
                ],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/track/track-static-sunrise" }
            },
            new()
            {
                Id = "track-solar-echo",
                Name = "Solar Echo",
                DurationMs = 176000,
                DiscNumber = 1,
                TrackNumber = 5,
                IsLocal = false,
                Artists = [neons, ember],
                Images =
                [
                    new Image { Url = "https://images.example.com/tracks/solar-echo.jpg", Height = 640, Width = 640 },
                    new Image { Url = "https://images.example.com/tracks/solar-echo-square.jpg", Height = 300, Width = 300 }
                ],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/track/track-solar-echo" }
            }
        };

        var albums = new List<Album>
        {
            new()
            {
                Id = "album-lunar-protocol",
                Name = "Lunar Protocol",
                AlbumType = "album",
                TotalTracks = 2,
                ReleaseDate = "2022-11-18",
                ReleaseDatePrecision = "day",
                Label = "Night Current Records",
                Popularity = 74,
                Artists = [luna],
                Images =
                [
                    new Image { Url = "https://images.example.com/albums/lunar-protocol-640.jpg", Height = 640, Width = 640 },
                    new Image { Url = "https://images.example.com/albums/lunar-protocol-300.jpg", Height = 300, Width = 300 }
                ],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/album/album-lunar-protocol" },
                Tracks = new AlbumTracksPage
                {
                    Href = "https://api.spotify.com/v1/albums/album-lunar-protocol/tracks",
                    Total = 2,
                    Limit = 2,
                    Offset = 0,
                    Next = "https://api.spotify.com/v1/albums/album-lunar-protocol/tracks?offset=2",
                    Previous = null,
                    Items = [tracks[0], tracks[1]]
                }
            },
            new()
            {
                Id = "album-feather-and-noise",
                Name = "Feather and Noise",
                AlbumType = "album",
                TotalTracks = 2,
                ReleaseDate = "2021-06-10",
                ReleaseDatePrecision = "day",
                Label = "Vector Harbor",
                Popularity = 61,
                Artists = [blackbird, ember],
                Images =
                [
                    new Image { Url = "https://images.example.com/albums/feather-and-noise-640.jpg", Height = 640, Width = 640 },
                    new Image { Url = "https://images.example.com/albums/feather-and-noise-64.jpg", Height = 64, Width = 64 }
                ],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/album/album-feather-and-noise" },
                Tracks = new AlbumTracksPage
                {
                    Href = "https://api.spotify.com/v1/albums/album-feather-and-noise/tracks",
                    Total = 2,
                    Limit = 1,
                    Offset = 1,
                    Next = "https://api.spotify.com/v1/albums/album-feather-and-noise/tracks?offset=2",
                    Previous = "https://api.spotify.com/v1/albums/album-feather-and-noise/tracks?offset=0",
                    Items = [tracks[2], tracks[3]]
                }
            },
            new()
            {
                Id = "album-cloud-garden-ep",
                Name = "Cloud Garden EP",
                AlbumType = "single",
                TotalTracks = 1,
                ReleaseDate = "2024-02-02",
                ReleaseDatePrecision = "day",
                Label = "Mirrorline",
                Popularity = 58,
                Artists = [neons],
                Images = [new Image { Url = "https://images.example.com/albums/cloud-garden-ep.jpg", Height = 640, Width = 640 }],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/album/album-cloud-garden-ep" },
                Tracks = new AlbumTracksPage
                {
                    Href = "https://api.spotify.com/v1/albums/album-cloud-garden-ep/tracks",
                    Total = 1,
                    Limit = 1,
                    Offset = 0,
                    Next = "https://api.spotify.com/v1/albums/album-cloud-garden-ep/tracks?offset=1",
                    Previous = "https://api.spotify.com/v1/albums/album-cloud-garden-ep/tracks?offset=0",
                    Items = [tracks[4]]
                }
            }
        };

        var playlists = new List<Playlist>
        {
            new()
            {
                Id = "playlist-night-drive",
                Name = "Night Drive",
                Description = "Synth-heavy tracks for late coding sessions",
                SnapshotId = "snapshot-001",
                Owner = new Owner
                {
                    DisplayName = "pg",
                    ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/user/pg" }
                },
                Images =
                [
                    new Image { Url = "https://images.example.com/playlists/night-drive.jpg", Height = 640, Width = 640 },
                    new Image { Url = "https://images.example.com/playlists/night-drive-thumb.jpg", Height = 300, Width = 300 }
                ],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/playlist/playlist-night-drive" },
                LastShuffled = DateTime.UtcNow.AddDays(-2),
                Tracks = new PlaylistTracksPage
                {
                    Href = "https://api.spotify.com/v1/playlists/playlist-night-drive/tracks",
                    Total = 3,
                    Limit = 100,
                    Offset = 0,
                    Next = "https://api.spotify.com/v1/playlists/playlist-night-drive/tracks?offset=3",
                    Previous = "https://api.spotify.com/v1/playlists/playlist-night-drive/tracks?offset=0",
                    Items =
                    [
                        new PlaylistTrack { Track = tracks[0] },
                        new PlaylistTrack { Track = tracks[1] },
                        new PlaylistTrack { Track = tracks[4] }
                    ]
                },
                Items = new PlaylistTracksPage
                {
                    Href = "https://api.spotify.com/v1/playlists/playlist-night-drive/tracks",
                    Total = 3,
                    Limit = 100,
                    Offset = 0,
                    Next = "https://api.spotify.com/v1/playlists/playlist-night-drive/tracks?offset=3",
                    Previous = "https://api.spotify.com/v1/playlists/playlist-night-drive/tracks?offset=0",
                    Items =
                    [
                        new PlaylistTrack { Track = tracks[0] },
                        new PlaylistTrack { Track = tracks[1] },
                        new PlaylistTrack { Track = tracks[4] }
                    ]
                }
            },
            new()
            {
                Id = "playlist-rainy-library",
                Name = "Rainy Library",
                Description = "Calmer cuts with longer runtimes",
                SnapshotId = "snapshot-002",
                Owner = new Owner
                {
                    DisplayName = "pg",
                    ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/user/pg" }
                },
                Images = [new Image { Url = "https://images.example.com/playlists/rainy-library.jpg", Height = 640, Width = 640 }],
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/playlist/playlist-rainy-library" },
                LastShuffled = DateTime.UtcNow.AddDays(-7),
                Tracks = new PlaylistTracksPage
                {
                    Href = "https://api.spotify.com/v1/playlists/playlist-rainy-library/tracks",
                    Total = 3,
                    Limit = 100,
                    Offset = 0,
                    Next = "https://api.spotify.com/v1/playlists/playlist-rainy-library/tracks?offset=3",
                    Previous = "https://api.spotify.com/v1/playlists/playlist-rainy-library/tracks?offset=-3",
                    Items =
                    [
                        new PlaylistTrack { Track = tracks[2] },
                        new PlaylistTrack { Track = tracks[3] },
                        new PlaylistTrack { Track = tracks[1] }
                    ]
                },
                Items = new PlaylistTracksPage
                {
                    Href = "https://api.spotify.com/v1/playlists/playlist-rainy-library/tracks",
                    Total = 3,
                    Limit = 100,
                    Offset = 0,
                    Next = "https://api.spotify.com/v1/playlists/playlist-rainy-library/tracks?offset=3",
                    Previous = "https://api.spotify.com/v1/playlists/playlist-rainy-library/tracks?offset=-3",
                    Items =
                    [
                        new PlaylistTrack { Track = tracks[2] },
                        new PlaylistTrack { Track = tracks[3] },
                        new PlaylistTrack { Track = tracks[1] }
                    ]
                }
            }
        };

        return new MusicDataSnapshot([luna, neons, blackbird, ember], albums, tracks, playlists);
    }
}

public sealed class MusicDataSnapshot(
    List<Artist> artists,
    List<Album> albums,
    List<Track> tracks,
    List<Playlist> playlists)
{
    public List<Artist> Artists { get; } = artists;

    public List<Album> Albums { get; } = albums;

    public List<Track> Tracks { get; } = tracks;

    public List<Playlist> Playlists { get; } = playlists;
}