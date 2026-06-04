using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public abstract class AlbumFormModel
{
	[Required(ErrorMessage = "Album name is required")]
	[StringLength(300, MinimumLength = 1, ErrorMessage = "Album name must be between 1 and 300 characters")]
	[Display(Name = "Album Name")]
	public string Name { get; set; } = string.Empty;

	[Required(ErrorMessage = "Album type is required")]
	[RegularExpression("^(album|single|compilation)$", ErrorMessage = "Album type must be one of: album, single, compilation")]
	[Display(Name = "Album Type")]
	public string AlbumType { get; set; } = string.Empty;

	[StringLength(100, ErrorMessage = "Label must not exceed 100 characters")]
	[Display(Name = "Label")]
	public string? Label { get; set; }

	[Range(0, 100, ErrorMessage = "Popularity must be between 0 and 100")]
	[Display(Name = "Popularity (0-100)")]
	public int Popularity { get; set; }

	[Required(ErrorMessage = "Release date is required")]
	[RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Release date must be in YYYY-MM-DD format")]
	[DataType(DataType.Date)]
	[Display(Name = "Release Date")]
	public string ReleaseDate { get; set; } = string.Empty;

	[MinLength(1, ErrorMessage = "Select at least one track")]
	[Display(Name = "Select Songs")]
	public List<string> TrackIds { get; set; } = [];

	[MinLength(1, ErrorMessage = "Select at least one artist")]
	[Display(Name = "Select Artists")]
	public List<string> ArtistIds { get; set; } = [];
}
