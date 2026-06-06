namespace spotify_swiss_knife.Models.Dtos;

public class PlaylistSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SpotifyUrl { get; set; }
    public int TrackCount { get; set; }

    public static PlaylistSummaryDto FromEntity(Playlist playlist) => new()
    {
        Id = playlist.Id,
        Name = playlist.Name,
        Description = playlist.Description,
        SpotifyUrl = playlist.ExternalUrls?.Spotify,
        TrackCount = playlist.TrackEntries?.Count ?? 0
    };
}
