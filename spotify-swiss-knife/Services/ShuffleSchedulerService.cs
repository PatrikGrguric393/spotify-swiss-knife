using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

public class ShuffleSchedulerService
{
    private readonly Dictionary<string, PendingShuffle> _pendingShuffles = new();
    private readonly object _lock = new();

    public class PendingShuffle
    {
        public string PlaylistId { get; set; } = string.Empty;
        public ShuffleRandomnessLevel RandomnessLevel { get; set; }
        public DateTime ScheduledFor { get; set; }
        public bool Executed { get; set; }
        public string? CompletionMessage { get; set; }
    }

    public void ScheduleShuffle(string playlistId, ShuffleRandomnessLevel randomnessLevel, DateTime scheduledFor)
    {
        lock (_lock)
        {
            var key = $"{playlistId}-{scheduledFor:O}";
            _pendingShuffles[key] = new PendingShuffle
            {
                PlaylistId = playlistId,
                RandomnessLevel = randomnessLevel,
                ScheduledFor = scheduledFor,
                Executed = false
            };
        }
    }

    public void MarkExecuted(string playlistId, string message)
    {
        lock (_lock)
        {
            var pending = _pendingShuffles.Values.FirstOrDefault(s => s.PlaylistId == playlistId && !s.Executed);
            if (pending is not null)
            {
                pending.Executed = true;
                pending.CompletionMessage = message;
            }
        }
    }

    public PendingShuffle? GetPendingShuffle(string playlistId)
    {
        lock (_lock)
        {
            return _pendingShuffles.Values.FirstOrDefault(s => s.PlaylistId == playlistId && !s.Executed);
        }
    }

    public (bool shouldExecute, PendingShuffle? shuffle) CheckAndGetDueShuffle(string playlistId)
    {
        lock (_lock)
        {
            var pending = GetPendingShuffle(playlistId);
            if (pending is null)
            {
                return (false, null);
            }

            if (DateTime.UtcNow >= pending.ScheduledFor && !pending.Executed)
            {
                return (true, pending);
            }

            return (false, pending);
        }
    }
}
