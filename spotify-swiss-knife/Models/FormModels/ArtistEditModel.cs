using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Form model for editing an existing Artist.
/// Includes ID to identify which artist is being edited.
/// </summary>
public class ArtistEditModel
{
	[Required]
	[Display(Name = "Artist ID")]
	public string Id { get; set; } = string.Empty;

	[Required(ErrorMessage = "Artist name is required")]
	[StringLength(300, MinimumLength = 1, ErrorMessage = "Artist name must be between 1 and 300 characters")]
	[Display(Name = "Artist Name")]
	public string Name { get; set; } = string.Empty;

	[Display(Name = "Spotify URL")]
	[Url(ErrorMessage = "Please enter a valid URL")]
	public string? SpotifyUrl { get; set; }
}
