namespace spotify_swiss_knife.Services;

// Produces a uniformly random ordering of a sequence. Kept separate from the Spotify
// API code so the shuffle algorithm can be reasoned about and unit-tested on its own.
public static class PlaylistShuffler
{
    // Returns a new list holding the same items in random order; the input is not mutated.
    public static List<T> Shuffle<T>(IReadOnlyList<T> items)
    {
        var shuffled = items.ToList();
        FisherYates(shuffled);
        return shuffled;
    }

    // Fisher-Yates: walk from the end, swapping each element with a uniformly chosen one at
    // or before its position. A single pass already yields a uniform permutation, so there
    // is no benefit to running it more than once. Random.Shared is thread-safe.
    private static void FisherYates<T>(List<T> list)
    {
        for (var index = list.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
        }
    }
}
