using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Shared, validated fields for the MVC track create/edit forms, backing
/// <see cref="TrackCreateForm"/> and <see cref="TrackEditForm"/>. Unlike the API DTO (which
/// takes raw milliseconds), the form accepts <see cref="Duration"/> as "mm:ss" or whole
/// minutes and the controller converts it to milliseconds.
/// </summary>
public abstract class TrackForm
{
    [Required(ErrorMessage = "Track name is required")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "Track name must be between 1 and 300 characters")]
    [Display(Name = "Track Name")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 500, ErrorMessage = "Track number must be between 0 and 500")]
    [Display(Name = "Track Number")]
    public int TrackNumber { get; set; }

    [Range(0, 5, ErrorMessage = "Disc number must be between 0 and 5")]
    [Display(Name = "Disc Number")]
    public int DiscNumber { get; set; } = 1;

    [Required(ErrorMessage = "Duration is required")]
    [RegularExpression(@"^\d+:[0-5]\d$|^\d+$", ErrorMessage = "Duration must be in mm:ss or mm format")]
    [Display(Name = "Duration")]
    public string Duration { get; set; } = string.Empty;

    [Display(Name = "Is Local")]
    public bool IsLocal { get; set; }

    [Display(Name = "Artists")]
    public List<string> ArtistIds { get; set; } = [];
}
