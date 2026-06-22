namespace spotify_swiss_knife.Models;

public sealed record BulkAlbumSaveResult(
    bool Succeeded,
    string? Error,
    int AlbumCount,
    int TrackCount,
    int PlaylistCount,
    IReadOnlyList<string> Warnings)
{
    public static BulkAlbumSaveResult Fail(string error) =>
        new(false, error, 0, 0, 0, []);
}
