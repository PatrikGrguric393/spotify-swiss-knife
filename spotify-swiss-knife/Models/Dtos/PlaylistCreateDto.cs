using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.Dtos;

public class PlaylistCreateDto
{
    [Required(ErrorMessage = "Playlist name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Playlist name must be between 1 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Owner name must not exceed 100 characters")]
    public string? OwnerDisplayName { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? SpotifyUrl { get; set; }

    public List<string> TrackIds { get; set; } = [];
}
