namespace spotify_swiss_knife.Models.Dtos;

/// <summary>
/// Full track projection for the detail view, including its album and artists.
/// Built via <see cref="FromEntity"/>.
/// </summary>
public class TrackDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public int DiscNumber { get; set; }
    public int TrackNumber { get; set; }
    public bool IsLocal { get; set; }
    public string? SpotifyUrl { get; set; }
    public AlbumListDto? Album { get; set; }
    public IReadOnlyCollection<ArtistListDto> Artists { get; set; } = new List<ArtistListDto>();

    public static TrackDetailDto FromEntity(Track track) => new()
    {
        Id = track.Id,
        Name = track.Name,
        DurationMs = track.DurationMs,
        DiscNumber = track.DiscNumber,
        TrackNumber = track.TrackNumber,
        IsLocal = track.IsLocal,
        SpotifyUrl = track.ExternalUrls?.Spotify,
        Album = track.Album != null ? AlbumListDto.FromEntity(track.Album) : null,
        Artists = track.Artists.Select(ArtistListDto.FromEntity).ToList()
    };
}
