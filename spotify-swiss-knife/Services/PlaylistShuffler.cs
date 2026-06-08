using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public static class PlaylistShuffler
{
    public static List<T> Shuffle<T>(IReadOnlyList<T> items, ShuffleRandomnessLevel randomnessLevel)
    {
        var shuffled = items.ToList();

        switch (randomnessLevel)
        {
            case ShuffleRandomnessLevel.Low:
                for (var index = 0; index < shuffled.Count - 1; index += 2)
                {
                    (shuffled[index], shuffled[index + 1]) = (shuffled[index + 1], shuffled[index]);
                }
                break;
            case ShuffleRandomnessLevel.High:
                FisherYates(shuffled);
                FisherYates(shuffled);
                FisherYates(shuffled);
                break;
            default:
                FisherYates(shuffled);
                break;
        }

        return shuffled;
    }

    private static void FisherYates<T>(List<T> list)
    {
        for (var index = list.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
        }
    }
}
