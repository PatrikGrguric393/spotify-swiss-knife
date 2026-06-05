using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public class TrackEditModel : TrackFormModel
{
	[Required]
	[Display(Name = "Track ID")]
	public string Id { get; set; } = string.Empty;
}
