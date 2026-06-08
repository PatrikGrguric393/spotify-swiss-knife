namespace spotify_swiss_knife.Models.Dtos;

public class PlaylistDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SpotifyUrl { get; set; }
    public string? OwnerDisplayName { get; set; }
    public IReadOnlyCollection<PlaylistTrackDto> Tracks { get; set; } = new List<PlaylistTrackDto>();

    public static PlaylistDetailDto FromEntity(Playlist playlist) => new()
    {
        Id = playlist.Id,
        Name = playlist.Name,
        Description = playlist.Description,
        SpotifyUrl = playlist.ExternalUrls?.Spotify,
        OwnerDisplayName = playlist.Owner?.DisplayName,
        Tracks = playlist.TrackEntries
            .OrderBy(e => e.SortOrder)
            .Select(e => new PlaylistTrackDto
            {
                Name = e.Track.Name,
                SpotifyUrl = e.Track.ExternalUrls?.Spotify
            })
            .ToList()
    };
}
