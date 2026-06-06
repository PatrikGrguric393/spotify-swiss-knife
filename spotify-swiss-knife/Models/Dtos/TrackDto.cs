namespace spotify_swiss_knife.Models.Dtos;

public class TrackDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public int DiscNumber { get; set; }
    public int TrackNumber { get; set; }
    public bool IsLocal { get; set; }
    public string? SpotifyUrl { get; set; }
    public AlbumSummaryDto? Album { get; set; }
    public IReadOnlyCollection<ArtistSummaryDto> Artists { get; set; } = new List<ArtistSummaryDto>();

    public static TrackDto FromEntity(Track track) => new()
    {
        Id = track.Id,
        Name = track.Name,
        DurationMs = track.DurationMs,
        DiscNumber = track.DiscNumber,
        TrackNumber = track.TrackNumber,
        IsLocal = track.IsLocal,
        SpotifyUrl = track.ExternalUrls?.Spotify,
        Album = track.Album != null ? AlbumSummaryDto.FromEntity(track.Album) : null,
        Artists = track.Artists.Select(ArtistSummaryDto.FromEntity).ToList()
    };
}
