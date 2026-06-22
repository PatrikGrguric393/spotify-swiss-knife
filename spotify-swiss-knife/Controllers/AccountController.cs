using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.FormModels;

namespace spotify_swiss_knife.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // A local account and Spotify are mutually exclusive: a local sign-in/register is
    // refused while Spotify is connected.
    private async Task<bool> IsSpotifyConnectedAsync() =>
        (await HttpContext.AuthenticateAsync(SpotifyAuthDefaults.Scheme)).Succeeded;

    private IActionResult RedirectToChooserForSpotifyConflict()
    {
        TempData["LoginError"] = "You're connected with Spotify. Disconnect first to use a local account instead.";
        return RedirectToAction("Index", "Login");
    }

    [HttpGet("register")]
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
            return LocalRedirect(returnUrl ?? "/");

        if (await IsSpotifyConnectedAsync())
            return RedirectToChooserForSpotifyConflict();

        ViewData["ReturnUrl"] = returnUrl;
        return View(new UserRegisterForm());
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(UserRegisterForm model, string? returnUrl = null)
    {
        if (await IsSpotifyConnectedAsync())
            return RedirectToChooserForSpotifyConflict();

        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            DateOfBirth = model.DateOfBirth,
            OIB = model.OIB.Trim(),
            JMBAG = model.JMBAG.Trim()
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Registration failed for {Email}: {Errors}.",
                model.Email, string.Join("; ", result.Errors.Select(e => e.Code)));
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "User");
        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("New user registered and signed in: {Email} ({UserId}).", user.Email, user.Id);
        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
            return LocalRedirect(returnUrl ?? "/");

        if (await IsSpotifyConnectedAsync())
            return RedirectToChooserForSpotifyConflict();

        ViewData["ReturnUrl"] = returnUrl;
        return View(new UserLoginForm());
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(UserLoginForm model, string? returnUrl = null)
    {
        if (await IsSpotifyConnectedAsync())
            return RedirectToChooserForSpotifyConflict();

        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed local login attempt for {Email}.", model.Email);
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        _logger.LogInformation("User logged in: {Email}.", model.Email);
        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var email = _userManager.GetUserName(User);
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out: {Email}.", email ?? "unknown");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("denied")]
    public IActionResult Denied() => View();

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        var rows = new List<UserRoleRow>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new UserRoleRow
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                OIB = user.OIB,
                JMBAG = user.JMBAG,
                CurrentRole = roles.FirstOrDefault() ?? string.Empty
            });
        }

        ViewBag.Roles = IdentitySeeder.Roles;
        return View(rows);
    }

    [HttpGet("users/{id}/edit")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = IdentitySeeder.Roles;
        return View(new UserEditForm
        {
            Id = user.Id,
            Email = user.Email ?? user.UserName ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            DateOfBirth = user.DateOfBirth,
            OIB = user.OIB,
            JMBAG = user.JMBAG,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }

    [HttpPost("users/{id}/edit")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(string id, UserEditForm model)
    {
        ViewBag.Roles = IdentitySeeder.Roles;
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.DateOfBirth = model.DateOfBirth;
        user.OIB = model.OIB.Trim();
        user.JMBAG = model.JMBAG.Trim();

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = model.Email;
            user.UserName = model.Email;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        if (IdentitySeeder.Roles.Contains(model.Role))
        {
            if (await WouldOrphanLastAdminAsync(user, model.Role))
            {
                TempData["UserError"] = "Cannot change the role of the last administrator.";
                return RedirectToAction(nameof(Users));
            }

            await AssignSingleRoleAsync(user, model.Role);
        }

        _logger.LogInformation("Admin {Admin} updated user {UserId} ({Email}), role set to {Role}.",
            _userManager.GetUserName(User), user.Id, user.Email, model.Role);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost("users/{id}/delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["UserError"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Users));
        }

        if (await WouldOrphanLastAdminAsync(user, newRole: null))
        {
            TempData["UserError"] = "Cannot delete the last administrator.";
            return RedirectToAction(nameof(Users));
        }

        await _userManager.DeleteAsync(user);
        _logger.LogInformation("Admin {Admin} deleted user {UserId} ({Email}).",
            _userManager.GetUserName(User), user.Id, user.Email);
        return RedirectToAction(nameof(Users));
    }

    private async Task AssignSingleRoleAsync(AppUser user, string role)
    {
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);
    }

    // True when the change would leave the system with zero admins.
    // newRole == null means the user is being removed entirely.
    private async Task<bool> WouldOrphanLastAdminAsync(AppUser user, string? newRole)
    {
        if (!await _userManager.IsInRoleAsync(user, "Admin"))
            return false;

        if (newRole == "Admin")
            return false;

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        return admins.Count <= 1;
    }
}
