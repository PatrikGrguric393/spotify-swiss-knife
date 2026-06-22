namespace spotify_swiss_knife.Models.FormModels;

public class BulkAlbumSaveForm
{
    public List<string> AlbumIds { get; set; } = [];

    public List<string> PlaylistIds { get; set; } = [];
}
