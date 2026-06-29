using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Filters;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Manages a user's recurring "scheduled shuffles": cron-driven jobs that re-shuffle chosen
// playlists on a schedule. Serves two sources from the same UI — a Spotify-connected user
// schedules their live Spotify playlists, while a local-account user schedules local-library
// playlists. Schedules persist to the database (tagged IsLocal) and the background
// ShuffleSchedulerService executes them. RequireServiceAuth gates the page.
[Route("schedules")]
[RequireServiceAuth]
public class SchedulesController : SpotifyControllerBase
{
    private readonly SpotifyDbContext _db;
    private readonly PlaylistRepository _playlistRepository;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(
        SpotifyDbContext db,
        PlaylistRepository playlistRepository,
        SpotifyAuthService spotifyAuth,
        ILogger<SchedulesController> logger) : base(spotifyAuth)
    {
        _db = db;
        _playlistRepository = playlistRepository;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var (userId, isLocal, _) = await ResolveServiceContextAsync();
        if (userId is null)
            return RedirectToAction("Index", "Login");

        var schedules = await _db.ScheduledShuffles
            .Where(s => s.UserId == userId && s.IsLocal == isLocal)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View(schedules);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var (userId, isLocal, accessToken) = await ResolveServiceContextAsync(withAccessToken: true);
        if (userId is null)
            return RedirectToAction("Index", "Login");
        if (!isLocal && accessToken is null)
            return RedirectToSpotifyLogin(Url.Action(nameof(Index)));

        ViewBag.Playlists = await GetEditablePlaylistsAsync(isLocal, accessToken);
        return View(new CreateScheduleForm());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateScheduleForm form)
    {
        var (userId, isLocal, accessToken) = await ResolveServiceContextAsync(withAccessToken: true);
        if (userId is null)
            return RedirectToAction("Index", "Login");
        if (!isLocal && accessToken is null)
            return RedirectToSpotifyLogin(Url.Action(nameof(Index)));

        if (!ModelState.IsValid)
        {
            ViewBag.Playlists = await GetEditablePlaylistsAsync(isLocal, accessToken);
            return View(form);
        }

        var cron = form.ToCronExpression();
        var nextRun = ShuffleSchedulerService.ComputeNextRun(cron, DateTimeOffset.UtcNow);
        if (nextRun is null)
        {
            ModelState.AddModelError(string.Empty, "Could not compute the next run time. Please check your schedule settings.");
            ViewBag.Playlists = await GetEditablePlaylistsAsync(isLocal, accessToken);
            return View(form);
        }

        var schedule = new ScheduledShuffle
        {
            UserId = userId,
            IsLocal = isLocal,
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
            "Schedule {Id} created by {UserId} (local={IsLocal}) for {Count} playlist(s) [{PlaylistIds}]; cron \"{Cron}\", next run {NextRun}.",
            schedule.Id, userId, isLocal, schedule.PlaylistIds.Count, string.Join(", ", schedule.PlaylistIds), cron, nextRun);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, isLocal, accessToken) = await ResolveServiceContextAsync(withAccessToken: true);
        if (userId is null)
            return RedirectToAction("Index", "Login");
        if (!isLocal && accessToken is null)
            return RedirectToSpotifyLogin(Url.Action(nameof(Index)));

        var schedule = await _db.ScheduledShuffles
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId && s.IsLocal == isLocal);
        if (schedule is null)
            return NotFound();

        // The stored cron is UTC; decode it into the form's fields as UTC values. The view
        // localizes the day/time to the viewer's timezone, the mirror image of how Create
        // converts local input to UTC via CreateScheduleForm.ToCronExpression.
        var decoded = CronScheduleDecoder.Decode(schedule.CronExpression);
        var form = new EditScheduleForm
        {
            Id = schedule.Id,
            PlaylistIds = schedule.PlaylistIds,
            PlaylistNames = schedule.PlaylistNames,
            Frequency = decoded.Frequency,
            DaysOfWeek = decoded.DaysOfWeek.ToList(),
            DayOfMonth = decoded.DayOfMonth,
            TimeUtc = $"{decoded.Hour:D2}:{decoded.Minute:D2}",
        };

        // Signals the view that the seeded day/time fields are UTC and must be converted to the
        // viewer's local timezone on load. A POST re-render (validation failure) does NOT set this,
        // because by then the fields already hold the user's local selection.
        ViewBag.LocalizeFromUtc = true;
        ViewBag.Playlists = await GetEditablePlaylistsAsync(isLocal, accessToken);
        return View(form);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] EditScheduleForm form)
    {
        var (userId, isLocal, accessToken) = await ResolveServiceContextAsync(withAccessToken: true);
        if (userId is null)
            return RedirectToAction("Index", "Login");
        if (!isLocal && accessToken is null)
            return RedirectToSpotifyLogin(Url.Action(nameof(Index)));

        var schedule = await _db.ScheduledShuffles
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId && s.IsLocal == isLocal);
        if (schedule is null)
            return NotFound();

        form.Id = id;

        if (!ModelState.IsValid)
        {
            ViewBag.Playlists = await GetEditablePlaylistsAsync(isLocal, accessToken);
            return View(form);
        }

        var cron = form.ToCronExpression();
        var nextRun = ShuffleSchedulerService.ComputeNextRun(cron, DateTimeOffset.UtcNow);
        if (nextRun is null)
        {
            ModelState.AddModelError(string.Empty, "Could not compute the next run time. Please check your schedule settings.");
            ViewBag.Playlists = await GetEditablePlaylistsAsync(isLocal, accessToken);
            return View(form);
        }

        schedule.PlaylistIds = form.PlaylistIds;
        schedule.PlaylistNames = form.PlaylistNames;
        schedule.CronExpression = cron;
        // Re-anchor the next run to the new schedule. A disabled schedule keeps the value but
        // won't fire until re-enabled.
        schedule.NextRunAt = nextRun;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Schedule {Id} updated by {UserId} (local={IsLocal}) for {Count} playlist(s) [{PlaylistIds}]; cron \"{Cron}\", next run {NextRun}.",
            schedule.Id, userId, isLocal, schedule.PlaylistIds.Count, string.Join(", ", schedule.PlaylistIds), cron, nextRun);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (schedule, error) = await FindOwnedScheduleAsync(id);
        if (error is not null) return error;

        schedule!.IsEnabled = !schedule.IsEnabled;
        if (schedule.IsEnabled)
            schedule.NextRunAt = ShuffleSchedulerService.ComputeNextRun(schedule.CronExpression, DateTimeOffset.UtcNow);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Schedule {Id} toggled to {State} by {UserId}.",
            schedule.Id, schedule.IsEnabled ? "enabled" : "disabled", schedule.UserId);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (schedule, error) = await FindOwnedScheduleAsync(id);
        if (error is not null) return error;

        _db.ScheduledShuffles.Remove(schedule!);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Schedule {Id} deleted by {UserId} ({Count} playlist(s)).",
            schedule!.Id, schedule.UserId, schedule.PlaylistIds.Count);

        return RedirectToAction(nameof(Index));
    }

    // A schedule re-shuffles playlists in place, so only playlists the user can edit are valid
    // targets: a local user's whole shared library, or (for Spotify) the playlists they own.
    private async Task<List<Playlist>> GetEditablePlaylistsAsync(bool isLocal, string? accessToken)
    {
        if (isLocal)
            return _playlistRepository.GetAll();

        if (accessToken is null)
            return [];

        var playlists = await SpotifyAuth.GetUserPlaylistsAsync(accessToken);
        if (playlists is null)
            return [];

        var profile = await SpotifyAuth.GetUserProfileAsync(accessToken);
        return FilterToOwnedPlaylists(playlists, profile);
    }

    private async Task<(ScheduledShuffle? Schedule, IActionResult? Error)> FindOwnedScheduleAsync(int id)
    {
        var (userId, isLocal, _) = await ResolveServiceContextAsync();
        if (userId is null)
            return (null, RedirectToAction("Index", "Login"));
        var schedule = await _db.ScheduledShuffles
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId && s.IsLocal == isLocal);
        return schedule is null ? (null, NotFound()) : (schedule, null);
    }

    // Resolves who the current request acts as and which source it targets. A Spotify session
    // takes precedence over a signed-in local account (the two are mutually exclusive anyway).
    // For local the UserId is the Identity user id. AccessToken is fetched lazily only for the
    // Spotify source (it can trigger a token refresh), so callers that don't need playlists
    // (Index/Toggle/Delete) never pay for it. UserId is null only if neither auth is present,
    // which RequireServiceAuth normally prevents.
    private async Task<(string? UserId, bool IsLocal, string? AccessToken)> ResolveServiceContextAsync(
        bool withAccessToken = false)
    {
        var spotifyUserId = await GetSpotifyUserIdAsync();
        if (spotifyUserId is not null)
        {
            var accessToken = withAccessToken
                ? await SpotifyAuth.GetValidAccessTokenAsync(spotifyUserId)
                : null;
            return (spotifyUserId, false, accessToken);
        }

        var localUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return (string.IsNullOrEmpty(localUserId) ? null : localUserId, true, null);
    }
}
