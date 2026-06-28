namespace spotify_swiss_knife.Models;

/// <summary>
/// View model for the bulk album-save page: the user's playlists to pick from, or an error
/// to show instead of the picker.
/// </summary>
public sealed class BulkAlbumSavePage
{
    public List<Playlist> Playlists { get; set; } = [];

    // Set when the page can't be used (e.g. playlists failed to load). When present the
    // view shows this instead of the picker.
    public string? ErrorMessage { get; set; }
}
