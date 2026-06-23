namespace spotify_swiss_knife.Models.FormModels;

/// <summary>
/// Submission for the bulk album-save: the selected album ids to copy into each of the
/// selected playlist ids.
/// </summary>
public class BulkAlbumSaveForm
{
    public List<string> AlbumIds { get; set; } = [];

    public List<string> PlaylistIds { get; set; } = [];
}
