namespace spotify_swiss_knife.Models.Dtos;

/// <summary>
/// Lightweight album projection for list/grid views. Built from an <see cref="Album"/> entity
/// via <see cref="FromEntity"/>; carries no nested artists or tracks.
/// </summary>
public sealed class AlbumListDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AlbumType { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public int TotalTracks { get; set; }
    public string? SpotifyUrl { get; set; }

    public static AlbumListDto FromEntity(Album album) => new()
    {
        Id = album.Id,
        Name = album.Name,
        AlbumType = album.AlbumType,
        ReleaseDate = album.ReleaseDate,
        TotalTracks = album.TotalTracks,
        SpotifyUrl = album.ExternalUrls.SpotifyUrl
    };
}
