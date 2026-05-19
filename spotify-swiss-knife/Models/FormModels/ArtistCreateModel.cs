using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Form model for creating a new Artist.
/// Only includes user-editable fields.
/// </summary>
public class ArtistCreateModel
{
	[Required(ErrorMessage = "Artist name is required")]
	[StringLength(300, MinimumLength = 1, ErrorMessage = "Artist name must be between 1 and 300 characters")]
	[Display(Name = "Artist Name")]
	public string Name { get; set; } = string.Empty;
}
