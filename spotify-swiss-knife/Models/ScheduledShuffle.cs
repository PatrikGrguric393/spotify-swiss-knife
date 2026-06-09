namespace spotify_swiss_knife.Models;

public class ScheduledShuffle
{
    public int Id { get; set; }

    // Spotify user ID — links to SpotifyToken.SpotifyUserId.
    public string UserId { get; set; } = string.Empty;

    public string PlaylistId { get; set; } = string.Empty;
    public string PlaylistName { get; set; } = string.Empty;
    public ShuffleRandomnessLevel RandomnessLevel { get; set; }

    // Standard 5-field cron expression (e.g. "0 8 * * 1" = Monday 08:00 UTC).
    public string CronExpression { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
