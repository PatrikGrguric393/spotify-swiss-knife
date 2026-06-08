namespace spotify_swiss_knife.Models.Dtos;

public class TrackListDto
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
        SpotifyUrl = track.ExternalUrls?.Spotify
    };
}
