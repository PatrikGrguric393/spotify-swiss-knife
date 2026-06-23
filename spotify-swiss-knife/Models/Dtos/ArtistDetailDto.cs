namespace spotify_swiss_knife.Models.Dtos;

/// <summary>
/// Full artist projection for the detail view, including the artist's albums and tracks.
/// Built via <see cref="FromEntity"/>.
/// </summary>
public class ArtistDetailDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? SpotifyUrl { get; set; }

    public IReadOnlyCollection<AlbumListDto> Albums { get; set; } = new List<AlbumListDto>();

    public IReadOnlyCollection<TrackListDto> Tracks { get; set; } = new List<TrackListDto>();

    public static ArtistDetailDto FromEntity(Artist artist) => new()
    {
        Id = artist.Id,
        Name = artist.Name,
        SpotifyUrl = artist.ExternalUrls?.Spotify,
        Albums = artist.Albums.Select(AlbumListDto.FromEntity).ToList(),
        Tracks = artist.Tracks.Select(TrackListDto.FromEntity).ToList()
    };
}
