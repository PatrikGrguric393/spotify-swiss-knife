using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models.FormModels;

public class AlbumEditForm : AlbumForm
{
	[Required]
	[Display(Name = "Album ID")]
	public string Id { get; set; } = string.Empty;

	[Display(Name = "Remove current cover")]
	public bool RemoveCoverImage { get; set; }

	public bool HasExistingCover { get; set; }
}
