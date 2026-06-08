namespace spotify_swiss_knife.Models.Dtos;

public class ArtistListDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? SpotifyUrl { get; set; }

    public int AlbumCount { get; set; }

    public int TrackCount { get; set; }

    public static ArtistListDto FromEntity(Artist artist) => new()
    {
        Id = artist.Id,
        Name = artist.Name,
        SpotifyUrl = artist.ExternalUrls?.Spotify,
        AlbumCount = artist.Albums?.Count ?? 0,
        TrackCount = artist.Tracks?.Count ?? 0
    };
}
