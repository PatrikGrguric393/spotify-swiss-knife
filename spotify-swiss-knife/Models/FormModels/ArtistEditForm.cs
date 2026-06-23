using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>Artist edit form: <see cref="ArtistForm"/> plus the artist id.</summary>
public class ArtistEditForm : ArtistForm
{
    [Required]
    [Display(Name = "Artist ID")]
    public string Id { get; set; } = string.Empty;
}
