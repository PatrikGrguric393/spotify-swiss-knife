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
			entity.OwnsOne(album => album.ExternalUrls);
			entity.OwnsMany(album => album.Images, image =>
			{
				image.WithOwner().HasForeignKey("AlbumId");
				image.Property<int>("Id");
				image.HasKey("AlbumId", "Id");
				image.ToTable("AlbumImages");
			});
		});

		modelBuilder.Entity<Artist>(entity =>
		{
			entity.HasKey(artist => artist.Id);
			entity.OwnsOne(artist => artist.ExternalUrls);
		});

		modelBuilder.Entity<Track>(entity =>
		{
			entity.HasKey(track => track.Id);
			entity.HasOne(track => track.Album)
				.WithMany(album => album.TrackList)
				.HasForeignKey(track => track.AlbumId)
				.OnDelete(DeleteBehavior.SetNull);
			entity.OwnsOne(track => track.ExternalUrls);
			entity.OwnsMany(track => track.Images, image =>
			{
				image.WithOwner().HasForeignKey("TrackId");
				image.Property<int>("Id");
				image.HasKey("TrackId", "Id");
				image.ToTable("TrackImages");
			});
		});

		modelBuilder.Entity<Playlist>(entity =>
		{
			entity.HasKey(playlist => playlist.Id);
			entity.OwnsOne(playlist => playlist.ExternalUrls);
			entity.OwnsOne(playlist => playlist.Owner, owner =>
			{
				owner.Property(o => o.DisplayName).HasColumnName("Owner_DisplayName");
				owner.OwnsOne(o => o.ExternalUrls, eu =>
				{
					eu.Property(x => x.Spotify).HasColumnName("Owner_ExternalUrls_Spotify");
				});
			});
			entity.OwnsMany(playlist => playlist.Images, image =>
			{
				image.WithOwner().HasForeignKey("PlaylistId");
				image.Property<int>("Id");
				image.HasKey("PlaylistId", "Id");
				image.ToTable("PlaylistImages");
			});
			entity.Ignore(playlist => playlist.Items);
			entity.Ignore(playlist => playlist.Tracks);
		});

		modelBuilder.Entity<PlaylistTrackEntry>(entity =>
		{
			entity.HasKey(entry => entry.Id);
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
				join => join.ToTable("AlbumArtists"));

		modelBuilder.Entity<Track>()
			.HasMany(track => track.Artists)
			.WithMany(artist => artist.Tracks)
			.UsingEntity<Dictionary<string, object>>(
				"TrackArtist",
				right => right.HasOne<Artist>().WithMany().HasForeignKey("ArtistId").OnDelete(DeleteBehavior.Cascade),
				left => left.HasOne<Track>().WithMany().HasForeignKey("TrackId").OnDelete(DeleteBehavior.Cascade),
				join => join.ToTable("TrackArtists"));
	}
}