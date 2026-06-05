using System.ComponentModel.DataAnnotations;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Models.FormModels;

public class ShufflePlaylistFormInput
{
    [Required]
    public string PlaylistId { get; set; } = string.Empty;

    public ShuffleRandomnessLevel RandomnessLevel { get; set; } = ShuffleRandomnessLevel.Medium;
}
