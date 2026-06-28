namespace spotify_swiss_knife.Models.Dtos;

/// <summary>
/// Full album projection for the detail view, including its artists and tracks. Note
/// <c>TotalTracks</c> reflects the loaded track set (<c>TrackList.Count</c>), unlike
/// <see cref="AlbumListDto"/> which uses the album's stored count.
/// </summary>
public sealed class AlbumDetailDto
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
    public IReadOnlyCollection<ArtistListDto> Artists { get; set; } = [];
    public IReadOnlyCollection<TrackListDto> Tracks { get; set; } = [];

    public static AlbumDetailDto FromEntity(Album album) => new()
    {
        Id = album.Id,
        Name = album.Name,
        AlbumType = album.AlbumType,
        ReleaseDate = album.ReleaseDate,
        ReleaseDatePrecision = album.ReleaseDatePrecision,
        TotalTracks = album.TrackList.Count,
        Label = album.Label,
        Popularity = album.Popularity,
        SpotifyUrl = album.ExternalUrls.SpotifyUrl,
        Artists = album.Artists.Select(ArtistListDto.FromEntity).ToList(),
        Tracks = album.TrackList.Select(TrackListDto.FromEntity).ToList()
    };
}
