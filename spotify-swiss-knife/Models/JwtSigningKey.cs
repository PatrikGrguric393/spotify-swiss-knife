namespace spotify_swiss_knife.Models;

/// <summary>
/// The persisted HMAC key used to sign the app's JWTs, generated once on first run and shared
/// across instances so tokens stay valid regardless of which instance issued them.
/// </summary>
public class JwtSigningKey
{
    public int Id { get; set; }

    // Logical key slot. A unique index on this column keeps exactly one row, so two
    // instances racing to generate the key on first run can't each persist a different one.
    public string Purpose { get; set; } = string.Empty;

    // Base64-encoded raw HMAC key bytes.
    public string KeyMaterial { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
