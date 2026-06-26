using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Drives the Spotify OAuth authorization-code flow under /auth: it sends the user to Spotify
// (login), handles the redirect back and validates the anti-forgery state (callback), shows a
// confirmation step (confirm), then signs the user in under the dedicated SpotifyConnect cookie
// scheme and persists their tokens (ConfirmPost). A signed-in local account blocks connecting
// Spotify, since the two are mutually exclusive. Logout disconnects Spotify.
[Route("auth")]
public class SpotifyAuthController : Controller
{
    private readonly SpotifyAuthService _spotifyAuth;
    private readonly ILogger<SpotifyAuthController> _logger;

    private const string StateCookieKey = "spotify_oauth_state";

    public SpotifyAuthController(SpotifyAuthService spotifyAuth, ILogger<SpotifyAuthController> logger)
    {
        _spotifyAuth = spotifyAuth;
        _logger = logger;
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if ((await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme)).Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        // A local account and Spotify are mutually exclusive: refuse to start the
        // Spotify flow while a local account is signed in.
        if (User.Identity?.IsAuthenticated == true)
        {
            TempData["LoginError"] = "You're signed in with a local account. Log out first to connect Spotify instead.";
            return RedirectToAction("Index", "Login");
        }

        var state = _spotifyAuth.GenerateState();
        Response.Cookies.Append(StateCookieKey, state, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            MaxAge = TimeSpan.FromMinutes(10),
        });

        return Redirect(_spotifyAuth.GetAuthorizationUrl(state));
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string? code, string? state, string? error)
    {
        if (error != null || code == null)
        {
            _logger.LogWarning("Spotify OAuth callback returned an error or no code: {Error}.", error ?? "missing code");
            return RedirectToAction("Index", "Home");
        }

        var expectedState = Request.Cookies[StateCookieKey];
        Response.Cookies.Delete(StateCookieKey);

        if (state == null || state != expectedState)
        {
            _logger.LogWarning("Spotify OAuth callback rejected: state mismatch.");
            return BadRequest("Invalid OAuth state. Please try logging in again.");
        }

        var tokens = await _spotifyAuth.ExchangeCodeAsync(code);
        if (tokens?.AccessToken == null || tokens.Error != null)
        {
            _logger.LogWarning("Spotify OAuth token exchange failed: {Error}.", tokens?.Error ?? "no access token");
            return RedirectToAction("Index", "Home");
        }

        var profile = await _spotifyAuth.GetUserProfileAsync(tokens.AccessToken);
        if (profile == null)
        {
            _logger.LogWarning("Spotify OAuth succeeded but profile fetch failed.");
            return RedirectToAction("Index", "Home");
        }

        TempData["auth_access_token"] = tokens.AccessToken;
        TempData["auth_refresh_token"] = tokens.RefreshToken ?? string.Empty;
        TempData["auth_expires_in"] = tokens.ExpiresIn.ToString();
        TempData["auth_user_id"] = profile.Id;
        TempData["auth_display_name"] = profile.DisplayName ?? profile.Id;
        TempData["auth_email"] = profile.Email ?? string.Empty;

        return RedirectToAction("Confirm");
    }

    [HttpGet("confirm")]
    public async Task<IActionResult> Confirm()
    {
        if (!TempData.ContainsKey("auth_access_token"))
            return RedirectToAction("Index", "Home");

        // Guard against a local account signing in between callback and confirm.
        if (User.Identity?.IsAuthenticated == true)
        {
            TempData["LoginError"] = "You're signed in with a local account. Log out first to connect Spotify instead.";
            return RedirectToAction("Index", "Login");
        }

        // Sign in as session-only immediately so the user is logged in even if they
        // close the page without clicking either button on the confirm view.
        TempData.Keep(); // preserve tokens for the optional persist-upgrade POST

        var accessToken = TempData["auth_access_token"]!.ToString()!;
        var refreshToken = TempData["auth_refresh_token"]?.ToString() ?? string.Empty;
        var expiresIn = int.TryParse(TempData["auth_expires_in"]?.ToString(), out var exp) ? exp : 3600;
        var userId = TempData["auth_user_id"]!.ToString()!;
        var displayName = TempData["auth_display_name"]?.ToString();
        var email = TempData["auth_email"]?.ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, displayName ?? userId),
            new(ClaimTypes.Email, email ?? string.Empty),
            new("access_token", accessToken),
            new("token_expiry", DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("O")),
        };

        if (!string.IsNullOrEmpty(refreshToken))
            claims.Add(new Claim("refresh_token", refreshToken));

        var identity = new ClaimsIdentity(claims, SpotifyAuthDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(SpotifyAuthDefaults.Scheme, principal,
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
        await _spotifyAuth.PersistTokensAsync(userId, accessToken, refreshToken, expiresIn);
        _logger.LogInformation("Spotify account connected: {DisplayName} ({UserId}), persistent: false (session default).",
            displayName ?? userId, userId);

        ViewData["DisplayName"] = displayName ?? "User";
        return View();
    }

    [HttpPost("confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPost(bool persist)
    {
        // Session-only sign-in already happened on the GET; only the persist upgrade needs work.
        if (!persist)
            return RedirectToAction("Index", "Home");

        var accessToken = TempData["auth_access_token"]?.ToString();
        var refreshToken = TempData["auth_refresh_token"]?.ToString() ?? string.Empty;
        var expiresIn = int.TryParse(TempData["auth_expires_in"]?.ToString(), out var exp) ? exp : 3600;
        var userId = TempData["auth_user_id"]?.ToString();
        var displayName = TempData["auth_display_name"]?.ToString();
        var email = TempData["auth_email"]?.ToString();

        // TempData may have expired (e.g. user waited too long); they're already signed in
        // session-only from the GET, so just send them home.
        if (accessToken == null || userId == null)
            return RedirectToAction("Index", "Home");

        // Guard against a local account signing in between callback and confirm.
        if (User.Identity?.IsAuthenticated == true)
        {
            TempData["LoginError"] = "You're signed in with a local account. Log out first to connect Spotify instead.";
            return RedirectToAction("Index", "Login");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, displayName ?? userId),
            new(ClaimTypes.Email, email ?? string.Empty),
            new("access_token", accessToken),
            new("token_expiry", DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("O")),
        };

        if (!string.IsNullOrEmpty(refreshToken))
            claims.Add(new Claim("refresh_token", refreshToken));

        var identity = new ClaimsIdentity(claims, SpotifyAuthDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(SpotifyAuthDefaults.Scheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                AllowRefresh = true,
            });
        _logger.LogInformation("Spotify account upgraded to persistent: {DisplayName} ({UserId}).",
            displayName ?? userId, userId);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var auth = await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme);
        var userId = auth.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        await HttpContext.SignOutAsync(SpotifyAuthDefaults.Scheme);
        _logger.LogInformation("Spotify account disconnected: {UserId}.", userId ?? "unknown");
        return RedirectToAction("Index", "Home");
    }
}
