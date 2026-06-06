using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.Dtos;

public class ArtistUpdateDto
{
    [Required(ErrorMessage = "Artist name is required")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "Artist name must be between 1 and 300 characters")]
    public string Name { get; set; } = string.Empty;

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? SpotifyUrl { get; set; }
}
