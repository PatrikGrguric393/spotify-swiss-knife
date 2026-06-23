namespace spotify_swiss_knife.Models.Dtos;

/// <summary>One track row within a <see cref="PlaylistDetailDto"/>.</summary>
public class PlaylistTrackDto
{
    public string Name { get; set; } = string.Empty;
    public string? SpotifyUrl { get; set; }
}
