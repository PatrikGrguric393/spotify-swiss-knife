using spotify_swiss_knife.Models.FormModels;

namespace spotify_swiss_knife.Models;

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
