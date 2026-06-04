using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public class ArtistEditModel : ArtistFormModel
{
	[Required]
	[Display(Name = "Artist ID")]
	public string Id { get; set; } = string.Empty;
}
