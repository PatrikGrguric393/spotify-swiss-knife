namespace spotify_swiss_knife.Models.Dtos;

/// <summary>
/// Lightweight track projection for list views and for nesting inside album/artist detail
/// DTOs. Built via <see cref="FromEntity"/>.
/// </summary>
public sealed class TrackListDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public string? SpotifyUrl { get; set; }

    public static TrackListDto FromEntity(Track track) => new()
    {
        Id = track.Id,
        Name = track.Name,
        DurationMs = track.DurationMs,
        SpotifyUrl = track.ExternalUrls.SpotifyUrl
    };
}
