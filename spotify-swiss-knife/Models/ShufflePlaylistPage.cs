using System.ComponentModel.DataAnnotations;

namespace spotify_swiss_knife.Models;

public class ShufflePlaylistFormInput
{
    [Required]
    public string PlaylistId { get; set; } = string.Empty;

    public ShuffleRandomnessLevel RandomnessLevel { get; set; } = ShuffleRandomnessLevel.Medium;

    public bool StartImmediately { get; set; } = true;

    public DateTime? StartAt { get; set; }
}

public class ShufflePlaylistPage
{
    public List<Playlist> Playlists { get; set; } = [];

    public ShufflePlaylistFormInput Input { get; set; } = new();

    public string StatusMessage { get; set; } = string.Empty;

    public static ShufflePlaylistPage Create(
        List<Playlist> playlists,
        ShufflePlaylistFormInput? input = null,
        string statusMessage = "")
    {
        return new ShufflePlaylistPage
        {
            Playlists = playlists,
            Input = input ?? new ShufflePlaylistFormInput(),
            StatusMessage = statusMessage
        };
    }
}
