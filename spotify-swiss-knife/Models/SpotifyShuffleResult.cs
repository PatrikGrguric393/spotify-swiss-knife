namespace spotify_swiss_knife.Models;

/// <summary>
/// Outcome of reordering a Spotify playlist. Use the factories: <see cref="Ok"/> for full
/// success, <see cref="Fail"/> for an upfront failure, and <see cref="Partial"/> when the
/// reorder failed midway (the playlist is intact but only partly shuffled).
/// </summary>
public sealed record SpotifyShuffleResult(bool Succeeded, int TrackCount, int MovedCount, string? Error)
{
    public static SpotifyShuffleResult Ok(int trackCount, int movedCount) =>
        new(true, trackCount, movedCount, null);

    public static SpotifyShuffleResult Fail(string error) =>
        new(false, 0, 0, error);

    // The reorder ran partway before a request failed; the playlist is intact but
    // only partially shuffled, so report how far it got alongside the error.
    public static SpotifyShuffleResult Partial(int trackCount, int movedCount, string error) =>
        new(false, trackCount, movedCount, error);
}
