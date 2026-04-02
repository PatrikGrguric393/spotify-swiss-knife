using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class ExampleMusicData
{
    public List<Artist> Artists { get; init; } = [];

    public List<Album> Albums { get; init; } = [];

    public List<Track> Tracks { get; init; } = [];

    public List<Playlist> Playlists { get; init; } = [];

    public static ExampleMusicData Create()
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
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/track/track-river-in-binary" }
            },
            new()
            {
                Id = "track-static-sunrise",
                Name = "Static Sunrise",
                DurationMs = 201000,
                DiscNumber = 1,
                TrackNumber = 4,
                IsLocal = false,
                Artists = [ember],
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
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/album/album-lunar-protocol" },
                Tracks = new AlbumTracksPage
                {
                    Total = 2,
                    Limit = 2,
                    Offset = 0,
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
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/album/album-feather-and-noise" },
                Tracks = new AlbumTracksPage
                {
                    Total = 2,
                    Limit = 2,
                    Offset = 0,
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
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/album/album-cloud-garden-ep" },
                Tracks = new AlbumTracksPage
                {
                    Total = 1,
                    Limit = 1,
                    Offset = 0,
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
                Owner = new Owner { DisplayName = "pg" },
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/playlist/playlist-night-drive" },
                Tracks = new PlaylistTracksPage
                {
                    Total = 3,
                    Limit = 100,
                    Offset = 0,
                    Items =
                    [
                        new PlaylistTrack { Track = tracks[0] },
                        new PlaylistTrack { Track = tracks[1] },
                        new PlaylistTrack { Track = tracks[4] }
                    ]
                },
                Items = new PlaylistTracksPage
                {
                    Total = 3,
                    Limit = 100,
                    Offset = 0,
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
                Owner = new Owner { DisplayName = "pg" },
                ExternalUrls = new ExternalUrls { Spotify = "https://open.spotify.com/playlist/playlist-rainy-library" },
                Tracks = new PlaylistTracksPage
                {
                    Total = 3,
                    Limit = 100,
                    Offset = 0,
                    Items =
                    [
                        new PlaylistTrack { Track = tracks[2] },
                        new PlaylistTrack { Track = tracks[3] },
                        new PlaylistTrack { Track = tracks[1] }
                    ]
                },
                Items = new PlaylistTracksPage
                {
                    Total = 3,
                    Limit = 100,
                    Offset = 0,
                    Items =
                    [
                        new PlaylistTrack { Track = tracks[2] },
                        new PlaylistTrack { Track = tracks[3] },
                        new PlaylistTrack { Track = tracks[1] }
                    ]
                }
            }
        };

        return new ExampleMusicData
        {
            Artists = [luna, neons, blackbird, ember],
            Albums = albums,
            Tracks = tracks,
            Playlists = playlists
        };
    }
}
