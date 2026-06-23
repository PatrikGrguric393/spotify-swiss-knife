using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Shared, validated fields for the MVC album create/edit forms. <see cref="AlbumCreateForm"/>
/// and <see cref="AlbumEditForm"/> derive from this; the [Display] names drive the rendered
/// labels. Unlike the API DTOs, the form takes an uploaded cover image rather than a URL.
/// </summary>
public abstract class AlbumForm
{
    [Required(ErrorMessage = "Album name is required")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "Album name must be between 1 and 300 characters")]
    [Display(Name = "Album Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Album type is required")]
    [Display(Name = "Album Type")]
    public string AlbumType { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Label must not exceed 100 characters")]
    [Display(Name = "Label")]
    public string? Label { get; set; }

    [Range(0, 100, ErrorMessage = "Popularity must be between 0 and 100")]
    [Display(Name = "Popularity (0-100)")]
    public int Popularity { get; set; }

    [Required(ErrorMessage = "Release date is required")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Release date must be in YYYY-MM-DD format")]
    [DataType(DataType.Date)]
    [Display(Name = "Release Date")]
    public string ReleaseDate { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Select at least one song")]
    [Display(Name = "Songs")]
    public List<string> TrackIds { get; set; } = [];

    [MinLength(1, ErrorMessage = "Select at least one artist")]
    [Display(Name = "Select Artists")]
    public List<string> ArtistIds { get; set; } = [];

    [Display(Name = "Album Cover")]
    public IFormFile? CoverImage { get; set; }
}
