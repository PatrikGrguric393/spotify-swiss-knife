using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>Track edit form: <see cref="TrackForm"/> plus the track id.</summary>
public class TrackEditForm : TrackForm
{
    [Required]
    [Display(Name = "Track ID")]
    public string Id { get; set; } = string.Empty;
}
