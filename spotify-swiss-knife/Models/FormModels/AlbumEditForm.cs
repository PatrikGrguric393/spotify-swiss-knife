using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Album edit form: <see cref="AlbumForm"/> plus the album id and cover-image controls.
/// <see cref="HasExistingCover"/> tells the view whether to offer removing the current cover,
/// and <see cref="RemoveCoverImage"/> requests that removal.
/// </summary>
public sealed class AlbumEditForm : AlbumForm
{
    [Required]
    [Display(Name = "Album ID")]
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Remove current cover")]
    public bool RemoveCoverImage { get; set; }

    public bool HasExistingCover { get; set; }
}
