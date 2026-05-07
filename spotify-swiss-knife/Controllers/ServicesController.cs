using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

public class ServicesController : Controller
{
    private readonly PlaylistRepository _playlistRepository;

    public ServicesController(
        PlaylistRepository playlistRepository
    )
    {
        _playlistRepository = playlistRepository;
    }

    [Route("/shuffle")]
    public IActionResult ShufflePlaylist()
    {
        var playlists = _playlistRepository.GetAll();
        var viewModel = ShufflePlaylistPage.Create(playlists);
        return View(viewModel);
    }

    [HttpPost("/shuffle")]
    [ValidateAntiForgeryToken]
    public IActionResult ShufflePlaylist([Bind(Prefix = "Input")] ShufflePlaylistFormInput input)
    {
        var playlists = _playlistRepository.GetAll();
        var selectedPlaylist = playlists.FirstOrDefault(playlist => playlist.Id == input.PlaylistId);

        if (selectedPlaylist is null)
        {
            ModelState.AddModelError(nameof(input.PlaylistId), "Please select a valid playlist.");
        }

        var statusMessage = string.Empty;

        if (ModelState.IsValid && selectedPlaylist is not null)
        {
            statusMessage = ExecuteShuffle(selectedPlaylist, input.RandomnessLevel);
            _playlistRepository.Update(selectedPlaylist);
        }

        var viewModel = ShufflePlaylistPage.Create(playlists, input, statusMessage);
        return View(viewModel);
    }

    private string ExecuteShuffle(Playlist playlist, ShuffleRandomnessLevel randomnessLevel)
    {
        var originalItems = playlist.Tracks.Items.ToList();
        var shuffledItems = ShuffleTracks(originalItems, randomnessLevel);

        if (originalItems.Count > 1 && HasSameOrder(originalItems, shuffledItems))
        {
            shuffledItems = RotateLeft(shuffledItems);
        }

        var originalPositions = originalItems
            .Select((item, index) => new { item.Track.Id, index })
            .ToDictionary(entry => entry.Id, entry => entry.index);

        var movedCount = shuffledItems
            .Select((item, index) => new { item.Track.Id, index })
            .Count(entry => originalPositions.TryGetValue(entry.Id, out var originalIndex) && originalIndex != entry.index);

        playlist.Tracks.Items = shuffledItems;
        playlist.LastShuffled = DateTime.UtcNow;

        return $"Shuffle completed for '{playlist.Name}'. " +
               $"Tracks: {shuffledItems.Count}, moved: {movedCount}, randomness: {randomnessLevel}, " +
               $"executed: {playlist.LastShuffled:yyyy-MM-dd HH:mm} UTC.";
    }

    private static List<PlaylistTrack> ShuffleTracks(List<PlaylistTrack> tracks, ShuffleRandomnessLevel randomnessLevel)
    {
        var shuffled = tracks.ToList();

        switch (randomnessLevel)
        {
            case ShuffleRandomnessLevel.Low:
                for (var index = 0; index < shuffled.Count - 1; index += 2)
                {
                    (shuffled[index], shuffled[index + 1]) = (shuffled[index + 1], shuffled[index]);
                }
                break;
            case ShuffleRandomnessLevel.Medium:
                FisherYatesShuffle(shuffled);
                break;
            case ShuffleRandomnessLevel.High:
                FisherYatesShuffle(shuffled);
                FisherYatesShuffle(shuffled);
                FisherYatesShuffle(shuffled);
                break;
            default:
                FisherYatesShuffle(shuffled);
                break;
        }

        return shuffled;
    }

    private static bool HasSameOrder(List<PlaylistTrack> original, List<PlaylistTrack> candidate)
    {
        if (original.Count != candidate.Count)
        {
            return false;
        }

        for (var index = 0; index < original.Count; index++)
        {
            if (original[index].Track.Id != candidate[index].Track.Id)
            {
                return false;
            }
        }

        return true;
    }

    private static List<PlaylistTrack> RotateLeft(List<PlaylistTrack> tracks)
    {
        if (tracks.Count <= 1)
        {
            return tracks;
        }

        var rotated = tracks.Skip(1).ToList();
        rotated.Add(tracks[0]);
        return rotated;
    }

    private static void FisherYatesShuffle(List<PlaylistTrack> tracks)
    {
        for (var index = tracks.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (tracks[index], tracks[swapIndex]) = (tracks[swapIndex], tracks[index]);
        }
    }
}

