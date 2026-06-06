using spotify_swiss_knife.Models.FormModels;

namespace spotify_swiss_knife.Models;

public class ShufflePlaylistPage
{
    public List<Playlist> Playlists { get; set; } = [];

    public ShufflePlaylistFormInput Input { get; set; } = new();

    public string StatusMessage { get; set; } = string.Empty;

    // Set when playlists can't be loaded (e.g. the user isn't logged in). When
    // present the view shows this instead of the shuffle form.
    public string ErrorMessage { get; set; } = string.Empty;

    // UTC instant of the last successful shuffle, rendered as ISO 8601 so the browser
    // can display it in the visitor's local format. Null when no shuffle has run.
    public DateTime? ShuffledAtUtc { get; set; }

    public static ShufflePlaylistPage Create(
        List<Playlist> playlists,
        ShufflePlaylistFormInput? input = null,
        string statusMessage = "",
        string? errorMessage = "",
        DateTime? shuffledAtUtc = null)
    {
        return new ShufflePlaylistPage
        {
            Playlists = playlists,
            Input = input ?? new ShufflePlaylistFormInput(),
            StatusMessage = statusMessage,
            ErrorMessage = errorMessage ?? string.Empty,
            ShuffledAtUtc = shuffledAtUtc
        };
    }
}
