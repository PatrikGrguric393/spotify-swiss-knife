namespace spotify_swiss_knife.Models.Dtos;

public class ArtistDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? SpotifyUrl { get; set; }

    public IReadOnlyCollection<AlbumSummaryDto> Albums { get; set; } = new List<AlbumSummaryDto>();

    public IReadOnlyCollection<TrackSummaryDto> Tracks { get; set; } = new List<TrackSummaryDto>();

    public static ArtistDto FromEntity(Artist artist) => new()
    {
        Id = artist.Id,
        Name = artist.Name,
        SpotifyUrl = artist.ExternalUrls?.Spotify,
        Albums = artist.Albums.Select(AlbumSummaryDto.FromEntity).ToList(),
        Tracks = artist.Tracks.Select(TrackSummaryDto.FromEntity).ToList()
    };
}
