using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using spotify_swiss_knife.Configuration;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

// Issues signed JWT access tokens for the CRUD API and manages the rotating set of
// refresh tokens backing them. Access tokens are stateless (validated by signature);
// refresh tokens are persisted (hashed) so they can be rotated and revoked.
public class JwtTokenService
{
    private readonly SpotifyDbContext _db;
    private readonly SigningKeyProvider _keyProvider;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;
    private readonly int _refreshTokenDays;

    public JwtTokenService(IOptions<JwtOptions> options, SpotifyDbContext db, SigningKeyProvider keyProvider)
    {
        _db = db;
        _keyProvider = keyProvider;
        var jwt = options.Value;
        _issuer = jwt.Issuer;
        _audience = jwt.Audience;
        _accessTokenMinutes = jwt.AccessTokenMinutes;
        _refreshTokenDays = jwt.RefreshTokenDays;
    }

    public (string Token, int ExpiresInSeconds) CreateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_accessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Id),
        };
        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(_keyProvider.GetSigningKey(), SecurityAlgorithms.HmacSha256));

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, (int)(expires - now).TotalSeconds);
    }

    // Mints a refresh token, stores only its hash, and returns the raw value to the caller.
    public async Task<string> IssueRefreshTokenAsync(string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var raw = GenerateRawToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = Hash(raw),
            UserId = userId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_refreshTokenDays),
        });
        await _db.SaveChangesAsync();

        return raw;
    }

    // Validates a presented refresh token and rotates it: the matched token is revoked and
    // a fresh one is issued. Returns the new raw token plus the owning user ID, or null if
    // the presented token is unknown, expired, or already used/revoked.
    public async Task<(string RefreshToken, string UserId)?> RotateRefreshTokenAsync(string rawToken)
    {
        var now = DateTimeOffset.UtcNow;
        var hash = Hash(rawToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (existing is null || !existing.IsActive(now))
            return null;

        existing.RevokedAt = now;
        var raw = GenerateRawToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = Hash(raw),
            UserId = existing.UserId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_refreshTokenDays),
        });
        await _db.SaveChangesAsync();

        return (raw, existing.UserId);
    }

    public async Task RevokeRefreshTokenAsync(string rawToken)
    {
        var hash = Hash(rawToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (existing is null || existing.RevokedAt is not null)
            return;

        existing.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static string GenerateRawToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
