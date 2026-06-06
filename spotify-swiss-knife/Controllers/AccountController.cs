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

    private const string SpotifyScheme = "SpotifyConnect";

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // A local account and Spotify are mutually exclusive: a local sign-in/register is
    // refused while Spotify is connected.
    private async Task<bool> IsSpotifyConnectedAsync() =>
        (await HttpContext.AuthenticateAsync(SpotifyScheme)).Succeeded;

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
        return View(new RegisterModel());
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model, string? returnUrl = null)
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
            DateOfBirth = model.DateOfBirth
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "User");
        await _signInManager.SignInAsync(user, isPersistent: false);
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
        return View(new LoginModel());
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
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
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
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
                CurrentRole = roles.FirstOrDefault() ?? string.Empty
            });
        }

        ViewBag.Roles = IdentitySeeder.Roles;
        return View(rows);
    }

    [HttpPost("users/role")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        if (!IdentitySeeder.Roles.Contains(role))
            return BadRequest("Unknown role.");

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        if (await WouldOrphanLastAdminAsync(user, role))
        {
            TempData["UserError"] = "Cannot change the role of the last administrator.";
            return RedirectToAction(nameof(Users));
        }

        await AssignSingleRoleAsync(user, role);
        return RedirectToAction(nameof(Users));
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
        return View(new EditUserModel
        {
            Id = user.Id,
            Email = user.Email ?? user.UserName ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            DateOfBirth = user.DateOfBirth,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }

    [HttpPost("users/{id}/edit")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(string id, EditUserModel model)
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
