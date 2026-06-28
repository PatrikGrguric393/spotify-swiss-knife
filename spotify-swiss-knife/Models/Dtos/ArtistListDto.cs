namespace spotify_swiss_knife.Models.Dtos;

/// <summary>
/// Lightweight artist projection for list views, with album/track counts instead of the full
/// collections. Built via <see cref="FromEntity"/>.
/// </summary>
public sealed class ArtistListDto
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
        SpotifyUrl = artist.ExternalUrls.SpotifyUrl,
        AlbumCount = artist.Albums.Count,
        TrackCount = artist.Tracks.Count
    };
}
