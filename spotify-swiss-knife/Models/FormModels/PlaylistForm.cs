using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Shared, validated fields for the MVC playlist create/edit forms, backing
/// <see cref="PlaylistCreateForm"/> and <see cref="PlaylistEditForm"/>.
/// </summary>
public abstract class PlaylistForm
{
    [Required(ErrorMessage = "Playlist name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Playlist name must be between 1 and 200 characters")]
    [Display(Name = "Playlist Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Owner name must not exceed 100 characters")]
    [Display(Name = "Owner")]
    public string? OwnerDisplayName { get; set; }

    [Display(Name = "Songs")]
    public List<string> TrackIds { get; set; } = [];
}
