using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.Dtos;

public class TrackCreateDto
{
    [Required(ErrorMessage = "Track name is required")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "Track name must be between 1 and 300 characters")]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Duration must be a positive number of milliseconds")]
    public int DurationMs { get; set; }

    [Range(1, 5, ErrorMessage = "Disc number must be between 1 and 5")]
    public int DiscNumber { get; set; } = 1;

    [Range(0, 500, ErrorMessage = "Track number must be between 0 and 500")]
    public int TrackNumber { get; set; }

    public bool IsLocal { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? SpotifyUrl { get; set; }

    public string? AlbumId { get; set; }
    public List<string> ArtistIds { get; set; } = [];
}
