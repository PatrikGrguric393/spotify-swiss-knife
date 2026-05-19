using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Form model for editing an existing Track.
/// Includes ID to identify which track is being edited.
/// </summary>
public class TrackEditModel
{
	[Required]
	[Display(Name = "Track ID")]
	public string Id { get; set; } = string.Empty;

	[Required(ErrorMessage = "Track name is required")]
	[StringLength(300, MinimumLength = 1, ErrorMessage = "Track name must be between 1 and 300 characters")]
	[Display(Name = "Track Name")]
	public string Name { get; set; } = string.Empty;

	[Range(0, 500, ErrorMessage = "Track number must be between 0 and 500")]
	[Display(Name = "Track Number")]
	public int TrackNumber { get; set; }

	[Range(0, 5, ErrorMessage = "Disc number must be between 0 and 5")]
	[Display(Name = "Disc Number")]
	public int DiscNumber { get; set; }

	[Range(0, 3600000, ErrorMessage = "Duration must be between 0 and 3600000 milliseconds (1 hour)")]
	[Display(Name = "Duration (milliseconds)")]
	public int DurationMs { get; set; }

	[Display(Name = "Is Local")]
	public bool IsLocal { get; set; }

	[Display(Name = "Album ID")]
	public string? AlbumId { get; set; }
}
