namespace spotify_swiss_knife.Models.Dtos;

/// <summary>One track row within a <see cref="PlaylistDetailDto"/>.</summary>
public sealed record PlaylistTrackDto
{
    public string Name { get; init; } = string.Empty;
    public string? SpotifyUrl { get; init; }

    public static PlaylistTrackDto FromTrack(Track track) => new()
    {
        Name = track.Name,
        SpotifyUrl = track.ExternalUrls.SpotifyUrl
    };
}
