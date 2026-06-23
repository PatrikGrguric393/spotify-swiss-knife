using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.Dtos;

/// <summary>
/// Shared, validated request body for creating or updating an album via the API. Create and
/// update currently take the same fields, so both <see cref="AlbumCreateDto"/> and
/// <see cref="AlbumUpdateDto"/> derive from this base (mirroring the Form model hierarchy).
/// </summary>
public abstract class AlbumWriteDto
{
    [Required(ErrorMessage = "Album name is required")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "Album name must be between 1 and 300 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Album type is required")]
    public string AlbumType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Release date is required")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Release date must be in YYYY-MM-DD format")]
    public string ReleaseDate { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Label must not exceed 100 characters")]
    public string? Label { get; set; }

    [Range(0, 100, ErrorMessage = "Popularity must be between 0 and 100")]
    public int Popularity { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? SpotifyUrl { get; set; }

    public List<string> ArtistIds { get; set; } = [];
    public List<string> TrackIds { get; set; } = [];
}
