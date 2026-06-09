namespace spotify_swiss_knife.Models;

public class RefreshToken
{
    public int Id { get; set; }

    // SHA-256 hash of the refresh token, never the raw value: a database leak must not
    // hand out usable long-lived credentials.
    public string TokenHash { get; set; } = string.Empty;

    // ASP.NET Identity user ID the token was issued to.
    public string UserId { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Set when the token is rotated out (on refresh) or explicitly revoked.
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
