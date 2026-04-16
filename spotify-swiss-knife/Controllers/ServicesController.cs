using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

public class ServicesController : Controller
{
    private readonly PlaylistMockRepository _playlistRepository;
    private readonly ShuffleSchedulerService _shuffleScheduler;

    public ServicesController(
        PlaylistMockRepository playlistRepository,
        ShuffleSchedulerService shuffleScheduler)
    {
        _playlistRepository = playlistRepository;
        _shuffleScheduler = shuffleScheduler;
    }

    public IActionResult ShufflePlaylist()
    {
        var playlists = _playlistRepository.GetAll();
        var viewModel = ShufflePlaylistPage.Create(playlists);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ShufflePlaylist(ShufflePlaylistFormInput input)
    {
        var playlists = _playlistRepository.GetAll();
        var selectedPlaylist = playlists.FirstOrDefault(playlist => playlist.Id == input.PlaylistId);

        if (selectedPlaylist is null)
        {
            ModelState.AddModelError(nameof(input.PlaylistId), "Please select a valid playlist.");
        }

        if (!input.StartImmediately && !input.StartAt.HasValue)
        {
            ModelState.AddModelError(nameof(input.StartAt), "Choose a start time or start immediately.");
        }

        var statusMessage = string.Empty;

        if (ModelState.IsValid && selectedPlaylist is not null)
        {
            if (input.StartAt.HasValue)
            {
                var scheduledForUtc = input.StartAt.Value.ToUniversalTime();
                input.StartImmediately = false;

                _shuffleScheduler.ScheduleShuffle(input.PlaylistId, input.RandomnessLevel, scheduledForUtc);
                statusMessage = $"Shuffle scheduled for '{selectedPlaylist.Name}' at {input.StartAt:yyyy-MM-dd HH:mm} (randomness: {input.RandomnessLevel}). Page will auto-refresh when complete.";
            }
            else if (input.StartImmediately)
            {
                statusMessage = ExecuteShuffle(selectedPlaylist, input.RandomnessLevel);
            }
        }

        var viewModel = ShufflePlaylistPage.Create(playlists, input, statusMessage);
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult CheckScheduledShuffle(string playlistId)
    {
        var (shouldExecute, pending) = _shuffleScheduler.CheckAndGetDueShuffle(playlistId);

        if (shouldExecute && pending is not null)
        {
            var playlists = _playlistRepository.GetAll();
            var playlist = playlists.FirstOrDefault(p => p.Id == playlistId);

            if (playlist is not null)
            {
                var message = ExecuteShuffle(playlist, pending.RandomnessLevel);
                _shuffleScheduler.MarkExecuted(playlistId, message);
                return Json(new { executed = true, message });
            }
        }

        return Json(new { executed = false, message = "No shuffle due." });
    }

    private string ExecuteShuffle(Playlist playlist, ShuffleRandomnessLevel randomnessLevel)
    {
        var originalItems = playlist.Tracks.Items.ToList();
        var shuffledItems = ShuffleTracks(originalItems, randomnessLevel);

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

    private static void FisherYatesShuffle(List<PlaylistTrack> tracks)
    {
        for (var index = tracks.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (tracks[index], tracks[swapIndex]) = (tracks[swapIndex], tracks[index]);
        }
    }
}
