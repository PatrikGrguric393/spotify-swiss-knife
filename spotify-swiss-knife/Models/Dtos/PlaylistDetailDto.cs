namespace spotify_swiss_knife.Models.Dtos;

/// <summary>
/// Full playlist projection for the detail view; tracks are ordered by their stored
/// <c>SortOrder</c>. Built via <see cref="FromEntity"/>.
/// </summary>
public sealed class PlaylistDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SpotifyUrl { get; set; }
    public string? OwnerDisplayName { get; set; }
    public IReadOnlyCollection<PlaylistTrackDto> Tracks { get; set; } = [];

    // Callers must eager-load TrackEntries with their Track navigation
    // (e.g. .Include(p => p.TrackEntries).ThenInclude(e => e.Track)) before calling this.
    public static PlaylistDetailDto FromEntity(Playlist playlist) => new()
    {
        Id = playlist.Id,
        Name = playlist.Name,
        Description = playlist.Description,
        SpotifyUrl = playlist.ExternalUrls.SpotifyUrl,
        OwnerDisplayName = playlist.Owner.DisplayName,
        Tracks = playlist.TrackEntries
            .OrderBy(e => e.SortOrder)
            .Select(e => PlaylistTrackDto.FromTrack(e.Track))
            .ToList()
    };
}
