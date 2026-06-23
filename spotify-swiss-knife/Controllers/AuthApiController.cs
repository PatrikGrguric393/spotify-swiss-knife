using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Models.Dtos;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

// Issues and manages JWT bearer tokens for the CRUD API under /api/auth. A local account
// exchanges its credentials for an access token plus a rotating refresh token (token), trades a
// refresh token for a new pair (refresh), or invalidates one (revoke). These are the tokens the
// api/* controllers authenticate with (see ApiControllerBase). This is separate from the
// interactive cookie sign-in handled by AccountController.
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtTokenService _tokens;

    public AuthApiController(UserManager<AppUser> userManager, JwtTokenService tokens)
    {
        _userManager = userManager;
        _tokens = tokens;
    }

    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Token([FromBody] TokenRequestDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.Username)
            ?? await _userManager.FindByEmailAsync(dto.Username);

        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { message = "Invalid username or password." });

        return Ok(await IssueAsync(user));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
    {
        var rotated = await _tokens.RotateRefreshTokenAsync(dto.RefreshToken);
        if (rotated is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        var user = await _userManager.FindByIdAsync(rotated.Value.UserId);
        if (user is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresIn) = _tokens.CreateAccessToken(user, roles);

        return Ok(new TokenResponseDto
        {
            AccessToken = accessToken,
            ExpiresIn = expiresIn,
            RefreshToken = rotated.Value.RefreshToken,
        });
    }

    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequestDto dto)
    {
        await _tokens.RevokeRefreshTokenAsync(dto.RefreshToken);
        return NoContent();
    }

    private async Task<TokenResponseDto> IssueAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresIn) = _tokens.CreateAccessToken(user, roles);
        var refreshToken = await _tokens.IssueRefreshTokenAsync(user.Id);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            ExpiresIn = expiresIn,
            RefreshToken = refreshToken,
        };
    }
}
