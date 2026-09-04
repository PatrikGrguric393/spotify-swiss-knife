using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

// Shuffles a local-library playlist's track order and persists it. Shared by the manual shuffle
// (ShuffleController) and the background scheduler (ShuffleSchedulerService) so both reorder and
// save identically. Local "is_local" tracks are only reordered, never re-added (PlaylistShuffler
// permutes the existing items in place).
public static class LocalPlaylistShuffle
{
    public readonly record struct Result(int TrackCount, int MovedCount, DateTime ShuffledAt);

    // Reorders the playlist's tracks, records LastShuffled, and persists both the new order
    // (PlaylistTrackEntry.SortOrder) and the timestamp via the repository.
    public static Result ShuffleAndSave(PlaylistRepository repository, Playlist playlist)
    {
        var originalItems = playlist.Tracks.Items.ToList();
        var shuffledItems = PlaylistShuffler.Shuffle(originalItems);

        var originalPositions = originalItems
            .Select((item, index) => new { item.Track.Id, index })
            .ToDictionary(e => e.Id, e => e.index);

        var moved = shuffledItems
            .Select((item, index) => new { item.Track.Id, index })
            .Count(e => originalPositions.TryGetValue(e.Id, out var orig) && orig != e.index);

        var shuffledAt = DateTime.UtcNow;
        playlist.Tracks.Items = shuffledItems;
        playlist.LastShuffled = shuffledAt;
        repository.Update(playlist);

        return new Result(shuffledItems.Count, moved, shuffledAt);
    }
}
