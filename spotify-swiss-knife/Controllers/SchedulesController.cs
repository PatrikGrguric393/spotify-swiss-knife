using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[Route("schedules")]
[RequireSpotifyAuth]
public class SchedulesController : Controller
{
    private readonly SpotifyDbContext _db;
    private readonly SpotifyAuthService _spotifyAuth;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(
        SpotifyDbContext db,
        SpotifyAuthService spotifyAuth,
        ILogger<SchedulesController> logger)
    {
        _db = db;
        _spotifyAuth = spotifyAuth;
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

        var playlists = await _spotifyAuth.GetUserPlaylistsAsync(accessToken);
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
            var playlists = await _spotifyAuth.GetUserPlaylistsAsync(accessToken);
            ViewBag.Playlists = playlists ?? [];
            return View(form);
        }

        var cron = form.ToCronExpression();
        var nextRun = ShuffleSchedulerService.ComputeNextRun(cron, DateTimeOffset.UtcNow);
        if (nextRun is null)
        {
            ModelState.AddModelError(string.Empty, "Could not compute the next run time. Please check your schedule settings.");
            var playlists = await _spotifyAuth.GetUserPlaylistsAsync(accessToken);
            ViewBag.Playlists = playlists ?? [];
            return View(form);
        }

        var schedule = new ScheduledShuffle
        {
            UserId = userId,
            PlaylistId = form.PlaylistId,
            PlaylistName = form.PlaylistName,
            RandomnessLevel = form.RandomnessLevel,
            CronExpression = cron,
            IsEnabled = true,
            NextRunAt = nextRun,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.ScheduledShuffles.Add(schedule);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Schedule {Id} created by {UserId} for playlist {PlaylistId} ({Name}); cron \"{Cron}\", next run {NextRun}.",
            schedule.Id, userId, schedule.PlaylistId, schedule.PlaylistName, cron, nextRun);

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

        _logger.LogInformation("Schedule {Id} deleted by {UserId} (playlist {PlaylistId}).",
            schedule.Id, userId, schedule.PlaylistId);

        return RedirectToAction(nameof(Index));
    }

    private async Task<string?> GetSpotifyUserIdAsync() =>
        (await GetSpotifyCredentialsAsync()).UserId;

    private async Task<(string? UserId, string? AccessToken)> GetSpotifyCredentialsAsync()
    {
        var auth = await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        if (!auth.Succeeded)
            return (null, null);
        var userId = auth.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var accessToken = auth.Principal?.FindFirstValue("access_token");
        return (userId, accessToken);
    }

    private IActionResult RedirectToLogin() =>
        RedirectToAction("Login", "SpotifyAuth", new { returnUrl = Url.Action("Index", "Schedules") });
}
