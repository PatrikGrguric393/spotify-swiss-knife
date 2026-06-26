using Cronos;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

// Background worker that runs scheduled playlist shuffles. Once a minute it loads every enabled
// schedule whose NextRunAt is due, shuffles its playlist using the owner's stored Spotify token,
// then advances NextRunAt from the schedule's cron expression. Each run is isolated: a failing
// schedule is logged and skipped without stopping the others, and NextRunAt is always advanced
// (in a finally) so a persistent failure can't make the same schedule fire on every tick.
public class ShuffleSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShuffleSchedulerService> _logger;

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(60);

    public ShuffleSchedulerService(IServiceScopeFactory scopeFactory, ILogger<ShuffleSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunDueSchedulesAsync(stoppingToken);
            await Task.Delay(TickInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunDueSchedulesAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SpotifyDbContext>();
        var spotifyAuth = scope.ServiceProvider.GetRequiredService<SpotifyAuthService>();

        var now = DateTimeOffset.UtcNow;
        var due = await db.ScheduledShuffles
            .Where(s => s.IsEnabled && s.NextRunAt <= now)
            .ToListAsync(ct);

        foreach (var schedule in due)
        {
            await RunScheduleAsync(schedule, db, spotifyAuth, now, ct);
        }
    }

    private async Task RunScheduleAsync(
        ScheduledShuffle schedule,
        SpotifyDbContext db,
        SpotifyAuthService spotifyAuth,
        DateTimeOffset now,
        CancellationToken ct)
    {
        try
        {
            var accessToken = await spotifyAuth.GetValidAccessTokenAsync(schedule.UserId);
            if (accessToken is null)
            {
                _logger.LogWarning(
                    "Scheduled shuffle {Id} skipped: no valid token for user {UserId}.",
                    schedule.Id, schedule.UserId);
                AdvanceNextRun(schedule, now);
                await db.SaveChangesAsync(ct);
                return;
            }

            // Shuffle every playlist in the schedule. One playlist's failure is logged and
            // skipped without aborting the rest, mirroring the manual multi-playlist shuffle.
            foreach (var playlistId in schedule.PlaylistIds)
            {
                ct.ThrowIfCancellationRequested();

                var result = await spotifyAuth.ShufflePlaylistAsync(accessToken, playlistId);

                if (result.Succeeded)
                {
                    _logger.LogInformation(
                        "Scheduled shuffle {Id} completed for playlist {PlaylistId}. Tracks: {Tracks}, moved: {Moved}.",
                        schedule.Id, playlistId, result.TrackCount, result.MovedCount);
                }
                else
                {
                    _logger.LogWarning(
                        "Scheduled shuffle {Id} failed for playlist {PlaylistId}: {Error}.",
                        schedule.Id, playlistId, result.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled error in scheduled shuffle {Id}.",
                schedule.Id);
        }
        finally
        {
            schedule.LastRunAt = now;
            AdvanceNextRun(schedule, now);
            await db.SaveChangesAsync(ct);
        }
    }

    // Computes the next occurrence after `after` and writes it to schedule.NextRunAt.
    // On parse failure the schedule is disabled to prevent a tight error loop.
    private void AdvanceNextRun(ScheduledShuffle schedule, DateTimeOffset after)
    {
        try
        {
            var cron = CronExpression.Parse(schedule.CronExpression);
            schedule.NextRunAt = cron.GetNextOccurrence(after, TimeZoneInfo.Utc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Invalid cron expression \"{Cron}\" on schedule {Id}; disabling.",
                schedule.CronExpression, schedule.Id);
            schedule.IsEnabled = false;
        }
    }

    // Computes the first next occurrence for a newly created or toggled schedule.
    public static DateTimeOffset? ComputeNextRun(string cronExpression, DateTimeOffset from)
    {
        try
        {
            var cron = CronExpression.Parse(cronExpression);
            return cron.GetNextOccurrence(from, TimeZoneInfo.Utc);
        }
        catch
        {
            return null;
        }
    }
}
