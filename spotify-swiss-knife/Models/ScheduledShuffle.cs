namespace spotify_swiss_knife.Models;

/// <summary>
/// A recurring, server-side shuffle for one or more playlists. ShuffleSchedulerService polls these
/// and reshuffles every listed playlist when the cron expression is next due.
/// </summary>
public class ScheduledShuffle
{
    public int Id { get; set; }

    // The owning user. For a Spotify schedule this is the Spotify user ID (links to
    // SpotifyToken.SpotifyUserId); for a local schedule it is the local Identity user ID.
    public string UserId { get; set; } = string.Empty;

    // True when this schedule shuffles local-library playlists (no Spotify token needed); false
    // when it shuffles the owner's live Spotify playlists. Determines which path the background
    // ShuffleSchedulerService takes, and scopes a user's schedule list to their own source.
    public bool IsLocal { get; set; }

    // Parallel lists: PlaylistNames[i] is the display name for PlaylistIds[i]. Stored as
    // PostgreSQL text[] columns. Order is preserved and the two arrays stay index-aligned.
    public List<string> PlaylistIds { get; set; } = [];
    public List<string> PlaylistNames { get; set; } = [];

    // Standard 5-field cron expression (e.g. "0 8 * * 1" = Monday 08:00 UTC).
    public string CronExpression { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
