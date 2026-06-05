using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public class PlaylistEditModel : PlaylistFormModel
{
	[Required]
	[Display(Name = "Playlist ID")]
	public string Id { get; set; } = string.Empty;
}
