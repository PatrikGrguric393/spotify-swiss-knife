using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>Playlist edit form: <see cref="PlaylistForm"/> plus the playlist id.</summary>
public sealed class PlaylistEditForm : PlaylistForm
{
    [Required]
    [Display(Name = "Playlist ID")]
    public string Id { get; set; } = string.Empty;
}
