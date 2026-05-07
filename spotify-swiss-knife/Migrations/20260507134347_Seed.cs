using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class Seed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Albums",
                columns: new[] { "Id", "AlbumType", "Label", "Name", "Popularity", "ReleaseDate", "ReleaseDatePrecision", "TotalTracks", "ExternalUrls_Spotify" },
                values: new object[,]
                {
                    { "album-cloud-garden-ep", "single", "Mirrorline", "Cloud Garden EP", 58, "2024-02-02", "day", 1, "https://open.spotify.com/album/album-cloud-garden-ep" },
                    { "album-feather-and-noise", "album", "Vector Harbor", "Feather and Noise", 61, "2021-06-10", "day", 2, "https://open.spotify.com/album/album-feather-and-noise" },
                    { "album-lunar-protocol", "album", "Night Current Records", "Lunar Protocol", 74, "2022-11-18", "day", 2, "https://open.spotify.com/album/album-lunar-protocol" }
                });

            migrationBuilder.InsertData(
                table: "Artists",
                columns: new[] { "Id", "Name", "ExternalUrls_Spotify" },
                values: new object[,]
                {
                    { "artist-blackbird-theory", "Blackbird Theory", "https://open.spotify.com/artist/artist-blackbird-theory" },
                    { "artist-ember-kite", "Ember Kite", "https://open.spotify.com/artist/artist-ember-kite" },
                    { "artist-luna-wave", "Luna Wave", "https://open.spotify.com/artist/artist-luna-wave" },
                    { "artist-neon-meadow", "Neon Meadow", "https://open.spotify.com/artist/artist-neon-meadow" }
                });

            migrationBuilder.InsertData(
                table: "Playlists",
                columns: new[] { "Id", "Owner_DisplayName", "Owner_ExternalUrls_Spotify", "Description", "LastShuffled", "Name", "SnapshotId", "ExternalUrls_Spotify" },
                values: new object[,]
                {
                    { "playlist-night-drive", "pg", "https://open.spotify.com/user/pg", "Synth-heavy tracks for late coding sessions", null, "Night Drive", "snapshot-001", "https://open.spotify.com/playlist/playlist-night-drive" },
                    { "playlist-rainy-library", "pg", "https://open.spotify.com/user/pg", "Calmer cuts with longer runtimes", null, "Rainy Library", "snapshot-002", "https://open.spotify.com/playlist/playlist-rainy-library" }
                });

            migrationBuilder.InsertData(
                table: "AlbumArtists",
                columns: new[] { "AlbumId", "ArtistId" },
                values: new object[,]
                {
                    { "album-cloud-garden-ep", "artist-neon-meadow" },
                    { "album-feather-and-noise", "artist-blackbird-theory" },
                    { "album-feather-and-noise", "artist-ember-kite" },
                    { "album-lunar-protocol", "artist-luna-wave" }
                });

            migrationBuilder.InsertData(
                table: "AlbumImages",
                columns: new[] { "AlbumId", "Id", "Height", "Url", "Width" },
                values: new object[,]
                {
                    { "album-cloud-garden-ep", 1, 640, "https://images.example.com/albums/cloud-garden-ep.jpg", 640 },
                    { "album-feather-and-noise", 1, 640, "https://images.example.com/albums/feather-and-noise-640.jpg", 640 },
                    { "album-feather-and-noise", 2, 64, "https://images.example.com/albums/feather-and-noise-64.jpg", 64 },
                    { "album-lunar-protocol", 1, 640, "https://images.example.com/albums/lunar-protocol-640.jpg", 640 },
                    { "album-lunar-protocol", 2, 300, "https://images.example.com/albums/lunar-protocol-300.jpg", 300 }
                });

            migrationBuilder.InsertData(
                table: "PlaylistImages",
                columns: new[] { "Id", "PlaylistId", "Height", "Url", "Width" },
                values: new object[,]
                {
                    { 1, "playlist-night-drive", 640, "https://images.example.com/playlists/night-drive.jpg", 640 },
                    { 2, "playlist-night-drive", 300, "https://images.example.com/playlists/night-drive-thumb.jpg", 300 },
                    { 1, "playlist-rainy-library", 640, "https://images.example.com/playlists/rainy-library.jpg", 640 }
                });

            migrationBuilder.InsertData(
                table: "Tracks",
                columns: new[] { "Id", "AlbumId", "DiscNumber", "DurationMs", "IsLocal", "Name", "TrackNumber", "ExternalUrls_Spotify" },
                values: new object[,]
                {
                    { "track-gravity-bloom", "album-lunar-protocol", 1, 188000, false, "Gravity Bloom", 2, "https://open.spotify.com/track/track-gravity-bloom" },
                    { "track-midnight-circuit", "album-lunar-protocol", 1, 213000, false, "Midnight Circuit", 1, "https://open.spotify.com/track/track-midnight-circuit" },
                    { "track-river-in-binary", "album-feather-and-noise", 1, 241000, false, "River in Binary", 3, "https://open.spotify.com/track/track-river-in-binary" },
                    { "track-solar-echo", "album-cloud-garden-ep", 1, 176000, false, "Solar Echo", 5, "https://open.spotify.com/track/track-solar-echo" },
                    { "track-static-sunrise", "album-feather-and-noise", 2, 201000, true, "Static Sunrise", 1, "https://open.spotify.com/track/track-static-sunrise" }
                });

            migrationBuilder.InsertData(
                table: "PlaylistTrackEntries",
                columns: new[] { "Id", "PlaylistId", "SortOrder", "TrackId" },
                values: new object[,]
                {
                    { 1, "playlist-night-drive", 0, "track-midnight-circuit" },
                    { 2, "playlist-night-drive", 1, "track-gravity-bloom" },
                    { 3, "playlist-night-drive", 2, "track-solar-echo" },
                    { 4, "playlist-rainy-library", 0, "track-river-in-binary" },
                    { 5, "playlist-rainy-library", 1, "track-static-sunrise" },
                    { 6, "playlist-rainy-library", 2, "track-gravity-bloom" }
                });

            migrationBuilder.InsertData(
                table: "TrackArtists",
                columns: new[] { "ArtistId", "TrackId" },
                values: new object[,]
                {
                    { "artist-blackbird-theory", "track-river-in-binary" },
                    { "artist-ember-kite", "track-solar-echo" },
                    { "artist-ember-kite", "track-static-sunrise" },
                    { "artist-luna-wave", "track-gravity-bloom" },
                    { "artist-luna-wave", "track-midnight-circuit" },
                    { "artist-neon-meadow", "track-gravity-bloom" },
                    { "artist-neon-meadow", "track-solar-echo" }
                });

            migrationBuilder.InsertData(
                table: "TrackImages",
                columns: new[] { "Id", "TrackId", "Height", "Url", "Width" },
                values: new object[,]
                {
                    { 1, "track-gravity-bloom", 640, "https://images.example.com/tracks/gravity-bloom.jpg", 640 },
                    { 1, "track-midnight-circuit", 640, "https://images.example.com/tracks/midnight-circuit-640.jpg", 640 },
                    { 2, "track-midnight-circuit", 300, "https://images.example.com/tracks/midnight-circuit-300.jpg", 300 },
                    { 1, "track-river-in-binary", null, "https://images.example.com/tracks/river-in-binary.jpg", null },
                    { 1, "track-solar-echo", 640, "https://images.example.com/tracks/solar-echo.jpg", 640 },
                    { 2, "track-solar-echo", 300, "https://images.example.com/tracks/solar-echo-square.jpg", 300 },
                    { 1, "track-static-sunrise", 512, "https://images.example.com/tracks/static-sunrise.jpg", 512 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AlbumArtists",
                keyColumns: new[] { "AlbumId", "ArtistId" },
                keyValues: new object[] { "album-cloud-garden-ep", "artist-neon-meadow" });

            migrationBuilder.DeleteData(
                table: "AlbumArtists",
                keyColumns: new[] { "AlbumId", "ArtistId" },
                keyValues: new object[] { "album-feather-and-noise", "artist-blackbird-theory" });

            migrationBuilder.DeleteData(
                table: "AlbumArtists",
                keyColumns: new[] { "AlbumId", "ArtistId" },
                keyValues: new object[] { "album-feather-and-noise", "artist-ember-kite" });

            migrationBuilder.DeleteData(
                table: "AlbumArtists",
                keyColumns: new[] { "AlbumId", "ArtistId" },
                keyValues: new object[] { "album-lunar-protocol", "artist-luna-wave" });

            migrationBuilder.DeleteData(
                table: "AlbumImages",
                keyColumns: new[] { "AlbumId", "Id" },
                keyValues: new object[] { "album-cloud-garden-ep", 1 });

            migrationBuilder.DeleteData(
                table: "AlbumImages",
                keyColumns: new[] { "AlbumId", "Id" },
                keyValues: new object[] { "album-feather-and-noise", 1 });

            migrationBuilder.DeleteData(
                table: "AlbumImages",
                keyColumns: new[] { "AlbumId", "Id" },
                keyValues: new object[] { "album-feather-and-noise", 2 });

            migrationBuilder.DeleteData(
                table: "AlbumImages",
                keyColumns: new[] { "AlbumId", "Id" },
                keyValues: new object[] { "album-lunar-protocol", 1 });

            migrationBuilder.DeleteData(
                table: "AlbumImages",
                keyColumns: new[] { "AlbumId", "Id" },
                keyValues: new object[] { "album-lunar-protocol", 2 });

            migrationBuilder.DeleteData(
                table: "PlaylistImages",
                keyColumns: new[] { "Id", "PlaylistId" },
                keyValues: new object[] { 1, "playlist-night-drive" });

            migrationBuilder.DeleteData(
                table: "PlaylistImages",
                keyColumns: new[] { "Id", "PlaylistId" },
                keyValues: new object[] { 2, "playlist-night-drive" });

            migrationBuilder.DeleteData(
                table: "PlaylistImages",
                keyColumns: new[] { "Id", "PlaylistId" },
                keyValues: new object[] { 1, "playlist-rainy-library" });

            migrationBuilder.DeleteData(
                table: "PlaylistTrackEntries",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PlaylistTrackEntries",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PlaylistTrackEntries",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "PlaylistTrackEntries",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "PlaylistTrackEntries",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "PlaylistTrackEntries",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "TrackArtists",
                keyColumns: new[] { "ArtistId", "TrackId" },
                keyValues: new object[] { "artist-blackbird-theory", "track-river-in-binary" });

            migrationBuilder.DeleteData(
                table: "TrackArtists",
                keyColumns: new[] { "ArtistId", "TrackId" },
                keyValues: new object[] { "artist-ember-kite", "track-solar-echo" });

            migrationBuilder.DeleteData(
                table: "TrackArtists",
                keyColumns: new[] { "ArtistId", "TrackId" },
                keyValues: new object[] { "artist-ember-kite", "track-static-sunrise" });

            migrationBuilder.DeleteData(
                table: "TrackArtists",
                keyColumns: new[] { "ArtistId", "TrackId" },
                keyValues: new object[] { "artist-luna-wave", "track-gravity-bloom" });

            migrationBuilder.DeleteData(
                table: "TrackArtists",
                keyColumns: new[] { "ArtistId", "TrackId" },
                keyValues: new object[] { "artist-luna-wave", "track-midnight-circuit" });

            migrationBuilder.DeleteData(
                table: "TrackArtists",
                keyColumns: new[] { "ArtistId", "TrackId" },
                keyValues: new object[] { "artist-neon-meadow", "track-gravity-bloom" });

            migrationBuilder.DeleteData(
                table: "TrackArtists",
                keyColumns: new[] { "ArtistId", "TrackId" },
                keyValues: new object[] { "artist-neon-meadow", "track-solar-echo" });

            migrationBuilder.DeleteData(
                table: "TrackImages",
                keyColumns: new[] { "Id", "TrackId" },
                keyValues: new object[] { 1, "track-gravity-bloom" });

            migrationBuilder.DeleteData(
                table: "TrackImages",
                keyColumns: new[] { "Id", "TrackId" },
                keyValues: new object[] { 1, "track-midnight-circuit" });

            migrationBuilder.DeleteData(
                table: "TrackImages",
                keyColumns: new[] { "Id", "TrackId" },
                keyValues: new object[] { 2, "track-midnight-circuit" });

            migrationBuilder.DeleteData(
                table: "TrackImages",
                keyColumns: new[] { "Id", "TrackId" },
                keyValues: new object[] { 1, "track-river-in-binary" });

            migrationBuilder.DeleteData(
                table: "TrackImages",
                keyColumns: new[] { "Id", "TrackId" },
                keyValues: new object[] { 1, "track-solar-echo" });

            migrationBuilder.DeleteData(
                table: "TrackImages",
                keyColumns: new[] { "Id", "TrackId" },
                keyValues: new object[] { 2, "track-solar-echo" });

            migrationBuilder.DeleteData(
                table: "TrackImages",
                keyColumns: new[] { "Id", "TrackId" },
                keyValues: new object[] { 1, "track-static-sunrise" });

            migrationBuilder.DeleteData(
                table: "Artists",
                keyColumn: "Id",
                keyValue: "artist-blackbird-theory");

            migrationBuilder.DeleteData(
                table: "Artists",
                keyColumn: "Id",
                keyValue: "artist-ember-kite");

            migrationBuilder.DeleteData(
                table: "Artists",
                keyColumn: "Id",
                keyValue: "artist-luna-wave");

            migrationBuilder.DeleteData(
                table: "Artists",
                keyColumn: "Id",
                keyValue: "artist-neon-meadow");

            migrationBuilder.DeleteData(
                table: "Playlists",
                keyColumn: "Id",
                keyValue: "playlist-night-drive");

            migrationBuilder.DeleteData(
                table: "Playlists",
                keyColumn: "Id",
                keyValue: "playlist-rainy-library");

            migrationBuilder.DeleteData(
                table: "Tracks",
                keyColumn: "Id",
                keyValue: "track-gravity-bloom");

            migrationBuilder.DeleteData(
                table: "Tracks",
                keyColumn: "Id",
                keyValue: "track-midnight-circuit");

            migrationBuilder.DeleteData(
                table: "Tracks",
                keyColumn: "Id",
                keyValue: "track-river-in-binary");

            migrationBuilder.DeleteData(
                table: "Tracks",
                keyColumn: "Id",
                keyValue: "track-solar-echo");

            migrationBuilder.DeleteData(
                table: "Tracks",
                keyColumn: "Id",
                keyValue: "track-static-sunrise");

            migrationBuilder.DeleteData(
                table: "Albums",
                keyColumn: "Id",
                keyValue: "album-cloud-garden-ep");

            migrationBuilder.DeleteData(
                table: "Albums",
                keyColumn: "Id",
                keyValue: "album-feather-and-noise");

            migrationBuilder.DeleteData(
                table: "Albums",
                keyColumn: "Id",
                keyValue: "album-lunar-protocol");
        }
    }
}
