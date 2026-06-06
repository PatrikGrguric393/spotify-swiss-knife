namespace spotify_swiss_knife.Models.Dtos;

public class AlbumDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AlbumType { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string ReleaseDatePrecision { get; set; } = string.Empty;
    public int TotalTracks { get; set; }
    public string? Label { get; set; }
    public int Popularity { get; set; }
    public string? SpotifyUrl { get; set; }
    public IReadOnlyCollection<ArtistSummaryDto> Artists { get; set; } = new List<ArtistSummaryDto>();
    public IReadOnlyCollection<TrackSummaryDto> Tracks { get; set; } = new List<TrackSummaryDto>();

    public static AlbumDto FromEntity(Album album) => new()
    {
        Id = album.Id,
        Name = album.Name,
        AlbumType = album.AlbumType,
        ReleaseDate = album.ReleaseDate,
        ReleaseDatePrecision = album.ReleaseDatePrecision,
        TotalTracks = album.TrackList.Count,
        Label = album.Label,
        Popularity = album.Popularity,
        SpotifyUrl = album.ExternalUrls?.Spotify,
        Artists = album.Artists.Select(ArtistSummaryDto.FromEntity).ToList(),
        Tracks = album.TrackList.Select(TrackSummaryDto.FromEntity).ToList()
    };
}
