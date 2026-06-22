namespace spotify_swiss_knife.Models;

public sealed class BulkAlbumSavePage
{
    public List<Playlist> Playlists { get; set; } = [];

    // Set when the page can't be used (e.g. playlists failed to load). When present the
    // view shows this instead of the picker.
    public string ErrorMessage { get; set; } = string.Empty;
}
