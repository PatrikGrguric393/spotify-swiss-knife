namespace spotify_swiss_knife.Models;

/// <summary>
/// Outcome of a bulk album-save: success counts plus any non-fatal <c>Warnings</c>, or a
/// failure carrying <c>Error</c>. Use <see cref="Fail"/> for the failure case.
/// </summary>
public sealed record BulkAlbumSaveResult(
    bool Succeeded,
    string? Error,
    int AlbumCount,
    int TrackCount,
    int PlaylistCount,
    IReadOnlyList<string> Warnings)
{
    public static BulkAlbumSaveResult Ok(int albumCount, int trackCount, int playlistCount, IReadOnlyList<string>? warnings = null) =>
        new(true, null, albumCount, trackCount, playlistCount, warnings ?? []);

    public static BulkAlbumSaveResult Fail(string error) =>
        new(false, error, 0, 0, 0, []);
}
