namespace spotify_swiss_knife.Models.Dtos;

public class TrackSummaryDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int DurationMs { get; set; }

    public static TrackSummaryDto FromEntity(Track track) => new()
    {
        Id = track.Id,
        Name = track.Name,
        DurationMs = track.DurationMs
    };
}
