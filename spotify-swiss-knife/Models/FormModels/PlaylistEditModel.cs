using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Form model for editing an existing Playlist.
/// Includes ID to identify which playlist is being edited.
/// </summary>
public class PlaylistEditModel
{
	[Required]
	[Display(Name = "Playlist ID")]
	public string Id { get; set; } = string.Empty;

	[Required(ErrorMessage = "Playlist name is required")]
	[StringLength(200, MinimumLength = 1, ErrorMessage = "Playlist name must be between 1 and 200 characters")]
	[Display(Name = "Playlist Name")]
	public string Name { get; set; } = string.Empty;

	[StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
	[Display(Name = "Description")]
	public string Description { get; set; } = string.Empty;
}
