using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Form model for editing an existing Album.
/// Includes ID to identify which album is being edited.
/// </summary>
public class AlbumEditModel
{
	[Required]
	[Display(Name = "Album ID")]
	public string Id { get; set; } = string.Empty;

	[Required(ErrorMessage = "Album name is required")]
	[StringLength(300, MinimumLength = 1, ErrorMessage = "Album name must be between 1 and 300 characters")]
	[Display(Name = "Album Name")]
	public string Name { get; set; } = string.Empty;

	[StringLength(50, ErrorMessage = "Album type must not exceed 50 characters")]
	[Display(Name = "Album Type")]
	public string AlbumType { get; set; } = string.Empty;

	[Range(0, 500, ErrorMessage = "Total tracks must be between 0 and 500")]
	[Display(Name = "Total Tracks")]
	public int TotalTracks { get; set; }

	[StringLength(100, ErrorMessage = "Label must not exceed 100 characters")]
	[Display(Name = "Label")]
	public string Label { get; set; } = string.Empty;

	[Range(0, 100, ErrorMessage = "Popularity must be between 0 and 100")]
	[Display(Name = "Popularity (0-100)")]
	public int Popularity { get; set; }

	[StringLength(10, ErrorMessage = "Release date precision must not exceed 10 characters")]
	[Display(Name = "Release Date Precision")]
	public string ReleaseDatePrecision { get; set; } = "day";

	[RegularExpression(@"^\d{4}-\d{2}-\d{2}$|^$", ErrorMessage = "Release date must be in YYYY-MM-DD format or empty")]
	[Display(Name = "Release Date (YYYY-MM-DD)")]
	public string ReleaseDate { get; set; } = string.Empty;
}
