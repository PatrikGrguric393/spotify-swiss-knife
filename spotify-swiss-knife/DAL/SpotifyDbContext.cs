using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.DAL;

public class SpotifyDbContext : DbContext
{
	public SpotifyDbContext(DbContextOptions<SpotifyDbContext> options) : base(options)
	{
	}

	public DbSet<Album> Albums => Set<Album>();
	public DbSet<Artist> Artists => Set<Artist>();
	public DbSet<Playlist> Playlists => Set<Playlist>();
	public DbSet<PlaylistTrackEntry> PlaylistTrackEntries => Set<PlaylistTrackEntry>();
	public DbSet<Track> Tracks => Set<Track>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Album>(entity =>
		{
			entity.HasKey(album => album.Id);
			entity.HasData(
				new { Id = "album-lunar-protocol", AlbumType = "album", TotalTracks = 2, Name = "Lunar Protocol", ReleaseDate = "2022-11-18", ReleaseDatePrecision = "day", Label = "Night Current Records", Popularity = 74 },
				new { Id = "album-feather-and-noise", AlbumType = "album", TotalTracks = 2, Name = "Feather and Noise", ReleaseDate = "2021-06-10", ReleaseDatePrecision = "day", Label = "Vector Harbor", Popularity = 61 },
				new { Id = "album-cloud-garden-ep", AlbumType = "single", TotalTracks = 1, Name = "Cloud Garden EP", ReleaseDate = "2024-02-02", ReleaseDatePrecision = "day", Label = "Mirrorline", Popularity = 58 });
			entity.OwnsOne(album => album.ExternalUrls);
			entity.OwnsOne(album => album.ExternalUrls).HasData(
				new { AlbumId = "album-lunar-protocol", Spotify = "https://open.spotify.com/album/album-lunar-protocol" },
				new { AlbumId = "album-feather-and-noise", Spotify = "https://open.spotify.com/album/album-feather-and-noise" },
				new { AlbumId = "album-cloud-garden-ep", Spotify = "https://open.spotify.com/album/album-cloud-garden-ep" });
			entity.OwnsMany(album => album.Images, image =>
			{
				image.WithOwner().HasForeignKey("AlbumId");
				image.Property<int>("Id");
				image.HasKey("AlbumId", "Id");
				image.ToTable("AlbumImages");
				image.HasData(
					new { AlbumId = "album-lunar-protocol", Id = 1, Url = "https://images.example.com/albums/lunar-protocol-640.jpg", Height = 640, Width = 640 },
					new { AlbumId = "album-lunar-protocol", Id = 2, Url = "https://images.example.com/albums/lunar-protocol-300.jpg", Height = 300, Width = 300 },
					new { AlbumId = "album-feather-and-noise", Id = 1, Url = "https://images.example.com/albums/feather-and-noise-640.jpg", Height = 640, Width = 640 },
					new { AlbumId = "album-feather-and-noise", Id = 2, Url = "https://images.example.com/albums/feather-and-noise-64.jpg", Height = 64, Width = 64 },
					new { AlbumId = "album-cloud-garden-ep", Id = 1, Url = "https://images.example.com/albums/cloud-garden-ep.jpg", Height = 640, Width = 640 });
			});
		});

		modelBuilder.Entity<Artist>(entity =>
		{
			entity.HasKey(artist => artist.Id);
			entity.HasData(
				new { Id = "artist-luna-wave", Name = "Luna Wave" },
				new { Id = "artist-neon-meadow", Name = "Neon Meadow" },
				new { Id = "artist-blackbird-theory", Name = "Blackbird Theory" },
				new { Id = "artist-ember-kite", Name = "Ember Kite" });
			entity.OwnsOne(artist => artist.ExternalUrls);
			entity.OwnsOne(artist => artist.ExternalUrls).HasData(
				new { ArtistId = "artist-luna-wave", Spotify = "https://open.spotify.com/artist/artist-luna-wave" },
				new { ArtistId = "artist-neon-meadow", Spotify = "https://open.spotify.com/artist/artist-neon-meadow" },
				new { ArtistId = "artist-blackbird-theory", Spotify = "https://open.spotify.com/artist/artist-blackbird-theory" },
				new { ArtistId = "artist-ember-kite", Spotify = "https://open.spotify.com/artist/artist-ember-kite" });
		});

		modelBuilder.Entity<Track>(entity =>
		{
			entity.HasKey(track => track.Id);
			entity.HasData(
				new { Id = "track-midnight-circuit", DiscNumber = 1, DurationMs = 213000, Name = "Midnight Circuit", TrackNumber = 1, IsLocal = false, AlbumId = "album-lunar-protocol" },
				new { Id = "track-gravity-bloom", DiscNumber = 1, DurationMs = 188000, Name = "Gravity Bloom", TrackNumber = 2, IsLocal = false, AlbumId = "album-lunar-protocol" },
				new { Id = "track-river-in-binary", DiscNumber = 1, DurationMs = 241000, Name = "River in Binary", TrackNumber = 3, IsLocal = false, AlbumId = "album-feather-and-noise" },
				new { Id = "track-static-sunrise", DiscNumber = 2, DurationMs = 201000, Name = "Static Sunrise", TrackNumber = 1, IsLocal = true, AlbumId = "album-feather-and-noise" },
				new { Id = "track-solar-echo", DiscNumber = 1, DurationMs = 176000, Name = "Solar Echo", TrackNumber = 5, IsLocal = false, AlbumId = "album-cloud-garden-ep" });
			entity.HasOne(track => track.Album)
				.WithMany(album => album.TrackList)
				.HasForeignKey(track => track.AlbumId)
				.OnDelete(DeleteBehavior.SetNull);
			entity.OwnsOne(track => track.ExternalUrls);
			entity.OwnsOne(track => track.ExternalUrls).HasData(
				new { TrackId = "track-midnight-circuit", Spotify = "https://open.spotify.com/track/track-midnight-circuit" },
				new { TrackId = "track-gravity-bloom", Spotify = "https://open.spotify.com/track/track-gravity-bloom" },
				new { TrackId = "track-river-in-binary", Spotify = "https://open.spotify.com/track/track-river-in-binary" },
				new { TrackId = "track-static-sunrise", Spotify = "https://open.spotify.com/track/track-static-sunrise" },
				new { TrackId = "track-solar-echo", Spotify = "https://open.spotify.com/track/track-solar-echo" });
			entity.OwnsMany(track => track.Images, image =>
			{
				image.WithOwner().HasForeignKey("TrackId");
				image.Property<int>("Id");
				image.HasKey("TrackId", "Id");
				image.ToTable("TrackImages");
				image.HasData(
					new { TrackId = "track-midnight-circuit", Id = 1, Url = "https://images.example.com/tracks/midnight-circuit-640.jpg", Height = 640, Width = 640 },
					new { TrackId = "track-midnight-circuit", Id = 2, Url = "https://images.example.com/tracks/midnight-circuit-300.jpg", Height = 300, Width = 300 },
					new { TrackId = "track-gravity-bloom", Id = 1, Url = "https://images.example.com/tracks/gravity-bloom.jpg", Height = 640, Width = 640 },
					new { TrackId = "track-river-in-binary", Id = 1, Url = "https://images.example.com/tracks/river-in-binary.jpg", Height = (int?)null, Width = (int?)null },
					new { TrackId = "track-static-sunrise", Id = 1, Url = "https://images.example.com/tracks/static-sunrise.jpg", Height = 512, Width = 512 },
					new { TrackId = "track-solar-echo", Id = 1, Url = "https://images.example.com/tracks/solar-echo.jpg", Height = 640, Width = 640 },
					new { TrackId = "track-solar-echo", Id = 2, Url = "https://images.example.com/tracks/solar-echo-square.jpg", Height = 300, Width = 300 });
			});
		});

		modelBuilder.Entity<Playlist>(entity =>
		{
			entity.HasKey(playlist => playlist.Id);
			entity.HasData(
				new { Id = "playlist-night-drive", Description = "Synth-heavy tracks for late coding sessions", Name = "Night Drive", SnapshotId = "snapshot-001", LastShuffled = (DateTime?)null },
				new { Id = "playlist-rainy-library", Description = "Calmer cuts with longer runtimes", Name = "Rainy Library", SnapshotId = "snapshot-002", LastShuffled = (DateTime?)null });
			entity.OwnsOne(playlist => playlist.ExternalUrls);
			entity.OwnsOne(playlist => playlist.ExternalUrls).HasData(
				new { PlaylistId = "playlist-night-drive", Spotify = "https://open.spotify.com/playlist/playlist-night-drive" },
				new { PlaylistId = "playlist-rainy-library", Spotify = "https://open.spotify.com/playlist/playlist-rainy-library" });
			entity.OwnsOne(playlist => playlist.Owner, owner =>
			{
				owner.Property(o => o.DisplayName).HasColumnName("Owner_DisplayName");
				owner.HasData(
					new { PlaylistId = "playlist-night-drive", DisplayName = "pg" },
					new { PlaylistId = "playlist-rainy-library", DisplayName = "pg" });
				owner.OwnsOne(o => o.ExternalUrls, eu =>
				{
					eu.Property(x => x.Spotify).HasColumnName("Owner_ExternalUrls_Spotify");
					eu.HasData(
						new { OwnerPlaylistId = "playlist-night-drive", Spotify = "https://open.spotify.com/user/pg" },
						new { OwnerPlaylistId = "playlist-rainy-library", Spotify = "https://open.spotify.com/user/pg" });
				});
			});
			entity.OwnsMany(playlist => playlist.Images, image =>
			{
				image.WithOwner().HasForeignKey("PlaylistId");
				image.Property<int>("Id");
				image.HasKey("PlaylistId", "Id");
				image.ToTable("PlaylistImages");
				image.HasData(
					new { PlaylistId = "playlist-night-drive", Id = 1, Url = "https://images.example.com/playlists/night-drive.jpg", Height = 640, Width = 640 },
					new { PlaylistId = "playlist-night-drive", Id = 2, Url = "https://images.example.com/playlists/night-drive-thumb.jpg", Height = 300, Width = 300 },
					new { PlaylistId = "playlist-rainy-library", Id = 1, Url = "https://images.example.com/playlists/rainy-library.jpg", Height = 640, Width = 640 });
			});
			entity.Ignore(playlist => playlist.Items);
			entity.Ignore(playlist => playlist.Tracks);
		});

		modelBuilder.Entity<PlaylistTrackEntry>(entity =>
		{
			entity.HasKey(entry => entry.Id);
			entity.HasData(
				new PlaylistTrackEntry { Id = 1, PlaylistId = "playlist-night-drive", TrackId = "track-midnight-circuit", SortOrder = 0 },
				new PlaylistTrackEntry { Id = 2, PlaylistId = "playlist-night-drive", TrackId = "track-gravity-bloom", SortOrder = 1 },
				new PlaylistTrackEntry { Id = 3, PlaylistId = "playlist-night-drive", TrackId = "track-solar-echo", SortOrder = 2 },
				new PlaylistTrackEntry { Id = 4, PlaylistId = "playlist-rainy-library", TrackId = "track-river-in-binary", SortOrder = 0 },
				new PlaylistTrackEntry { Id = 5, PlaylistId = "playlist-rainy-library", TrackId = "track-static-sunrise", SortOrder = 1 },
				new PlaylistTrackEntry { Id = 6, PlaylistId = "playlist-rainy-library", TrackId = "track-gravity-bloom", SortOrder = 2 });
			entity.HasOne(entry => entry.Playlist)
				.WithMany(playlist => playlist.TrackEntries)
				.HasForeignKey(entry => entry.PlaylistId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(entry => entry.Track)
				.WithMany()
				.HasForeignKey(entry => entry.TrackId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<Album>()
			.HasMany(album => album.Artists)
			.WithMany(artist => artist.Albums)
			.UsingEntity<Dictionary<string, object>>(
				"AlbumArtist",
				right => right.HasOne<Artist>().WithMany().HasForeignKey("ArtistId").OnDelete(DeleteBehavior.Cascade),
				left => left.HasOne<Album>().WithMany().HasForeignKey("AlbumId").OnDelete(DeleteBehavior.Cascade),
				join =>
				{
					join.ToTable("AlbumArtists");
					join.HasData(
						new { AlbumId = "album-lunar-protocol", ArtistId = "artist-luna-wave" },
						new { AlbumId = "album-feather-and-noise", ArtistId = "artist-blackbird-theory" },
						new { AlbumId = "album-feather-and-noise", ArtistId = "artist-ember-kite" },
						new { AlbumId = "album-cloud-garden-ep", ArtistId = "artist-neon-meadow" });
				});

		modelBuilder.Entity<Track>()
			.HasMany(track => track.Artists)
			.WithMany(artist => artist.Tracks)
			.UsingEntity<Dictionary<string, object>>(
				"TrackArtist",
				right => right.HasOne<Artist>().WithMany().HasForeignKey("ArtistId").OnDelete(DeleteBehavior.Cascade),
				left => left.HasOne<Track>().WithMany().HasForeignKey("TrackId").OnDelete(DeleteBehavior.Cascade),
				join =>
				{
					join.ToTable("TrackArtists");
					join.HasData(
						new { ArtistId = "artist-luna-wave", TrackId = "track-midnight-circuit" },
						new { ArtistId = "artist-luna-wave", TrackId = "track-gravity-bloom" },
						new { ArtistId = "artist-neon-meadow", TrackId = "track-gravity-bloom" },
						new { ArtistId = "artist-blackbird-theory", TrackId = "track-river-in-binary" },
						new { ArtistId = "artist-ember-kite", TrackId = "track-static-sunrise" },
						new { ArtistId = "artist-neon-meadow", TrackId = "track-solar-echo" },
						new { ArtistId = "artist-ember-kite", TrackId = "track-solar-echo" });
				});
	}
}