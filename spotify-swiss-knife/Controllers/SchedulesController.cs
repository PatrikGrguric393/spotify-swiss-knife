using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Manages a Spotify user's recurring "scheduled shuffles": cron-driven jobs that re-shuffle a
// chosen playlist on a schedule. Persists schedules to the database; the actual execution is
// driven by the background ShuffleSchedulerService. Requires a live Spotify connection.
[Route("schedules")]
[RequireSpotifyAuth]
public class SchedulesController : SpotifyControllerBase
{
    private readonly SpotifyDbContext _db;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(
        SpotifyDbContext db,
        SpotifyAuthService spotifyAuth,
        ILogger<SchedulesController> logger) : base(spotifyAuth)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = await GetSpotifyUserIdAsync();
        if (userId is null)
            return RedirectToLogin();

        var schedules = await _db.ScheduledShuffles
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View(schedules);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var (userId, accessToken) = await GetSpotifyCredentialsAsync();
        if (userId is null || accessToken is null)
            return RedirectToLogin();

        var playlists = await SpotifyAuth.GetUserPlaylistsAsync(accessToken);
        ViewBag.Playlists = playlists ?? [];
        return View(new CreateScheduleForm());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateScheduleForm form)
    {
        var (userId, accessToken) = await GetSpotifyCredentialsAsync();
        if (userId is null || accessToken is null)
            return RedirectToLogin();

        if (!ModelState.IsValid)
        {
            var playlists = await SpotifyAuth.GetUserPlaylistsAsync(accessToken);
            ViewBag.Playlists = playlists ?? [];
            return View(form);
        }

        var cron = form.ToCronExpression();
        var nextRun = ShuffleSchedulerService.ComputeNextRun(cron, DateTimeOffset.UtcNow);
        if (nextRun is null)
        {
            ModelState.AddModelError(string.Empty, "Could not compute the next run time. Please check your schedule settings.");
            var playlists = await SpotifyAuth.GetUserPlaylistsAsync(accessToken);
            ViewBag.Playlists = playlists ?? [];
            return View(form);
        }

        var schedule = new ScheduledShuffle
        {
            UserId = userId,
            PlaylistIds = form.PlaylistIds,
            PlaylistNames = form.PlaylistNames,
            CronExpression = cron,
            IsEnabled = true,
            NextRunAt = nextRun,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.ScheduledShuffles.Add(schedule);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Schedule {Id} created by {UserId} for {Count} playlist(s) [{PlaylistIds}]; cron \"{Cron}\", next run {NextRun}.",
            schedule.Id, userId, schedule.PlaylistIds.Count, string.Join(", ", schedule.PlaylistIds), cron, nextRun);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var userId = await GetSpotifyUserIdAsync();
        if (userId is null)
            return RedirectToLogin();

        var schedule = await _db.ScheduledShuffles
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (schedule is null)
            return NotFound();

        schedule.IsEnabled = !schedule.IsEnabled;
        if (schedule.IsEnabled)
            schedule.NextRunAt = ShuffleSchedulerService.ComputeNextRun(schedule.CronExpression, DateTimeOffset.UtcNow);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Schedule {Id} toggled to {State} by {UserId}.",
            schedule.Id, schedule.IsEnabled ? "enabled" : "disabled", userId);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = await GetSpotifyUserIdAsync();
        if (userId is null)
            return RedirectToLogin();

        var schedule = await _db.ScheduledShuffles
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (schedule is null)
            return NotFound();

        _db.ScheduledShuffles.Remove(schedule);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Schedule {Id} deleted by {UserId} ({Count} playlist(s)).",
            schedule.Id, userId, schedule.PlaylistIds.Count);

        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToLogin() =>
        RedirectToAction("Login", "SpotifyAuth", new { returnUrl = Url.Action("Index", "Schedules") });
}
