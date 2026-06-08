using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public class TrackEditForm : TrackForm
{
	[Required]
	[Display(Name = "Track ID")]
	public string Id { get; set; } = string.Empty;
}
