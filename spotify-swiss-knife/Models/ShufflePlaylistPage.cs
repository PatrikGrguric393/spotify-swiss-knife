using spotify_swiss_knife.Models.FormModels;

namespace spotify_swiss_knife.Models;

/// <summary>
/// View model for the manual shuffle page: the playlists to choose from, the submitted form,
/// and feedback (status, error, last-shuffled time). Build it with <see cref="Create"/>.
/// </summary>
public sealed class ShufflePlaylistPage
{
    public List<Playlist> Playlists { get; set; } = [];

    public PlaylistShuffleForm Input { get; set; } = new();

    public string? StatusMessage { get; set; }

    // Set when playlists can't be loaded (e.g. the user isn't logged in). When
    // present the view shows this instead of the shuffle form.
    public string? ErrorMessage { get; set; }

    // UTC instant of the last successful shuffle, rendered as ISO 8601 so the browser
    // can display it in the visitor's local format. Null when no shuffle has run.
    public DateTime? ShuffledAtUtc { get; set; }

    public static ShufflePlaylistPage Create(
        List<Playlist> playlists,
        PlaylistShuffleForm? input = null,
        string? statusMessage = null,
        string? errorMessage = null,
        DateTime? shuffledAtUtc = null)
    {
        return new ShufflePlaylistPage
        {
            Playlists = playlists,
            Input = input ?? new PlaylistShuffleForm(),
            StatusMessage = statusMessage,
            ErrorMessage = errorMessage,
            ShuffledAtUtc = shuffledAtUtc
        };
    }
}
