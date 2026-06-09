using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;

namespace spotify_swiss_knife.Services;

// Supplies the HS256 signing key for API JWTs. The key is generated once and persisted in
// the database, so it survives restarts and is shared across instances without living in
// config. Loaded lazily and cached for the process lifetime.
public class SigningKeyProvider
{
    private const string Purpose = "jwt-hs256";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _gate = new();
    private SymmetricSecurityKey? _cached;

    public SigningKeyProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public SymmetricSecurityKey GetSigningKey()
    {
        if (_cached is not null)
            return _cached;

        lock (_gate)
        {
            _cached ??= new SymmetricSecurityKey(LoadOrCreate());
            return _cached;
        }
    }

    private byte[] LoadOrCreate()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SpotifyDbContext>();

        var existing = db.JwtSigningKeys.FirstOrDefault(k => k.Purpose == Purpose);
        if (existing is not null)
            return Convert.FromBase64String(existing.KeyMaterial);

        var bytes = new byte[64]; // 512-bit key, comfortably above the HS256 minimum.
        RandomNumberGenerator.Fill(bytes);
        db.JwtSigningKeys.Add(new JwtSigningKey
        {
            Purpose = Purpose,
            KeyMaterial = Convert.ToBase64String(bytes),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        try
        {
            db.SaveChanges();
            return bytes;
        }
        catch (DbUpdateException)
        {
            // Another instance won the race; the unique Purpose index rejected our insert.
            // Re-read and use the key that landed.
            db.ChangeTracker.Clear();
            var winner = db.JwtSigningKeys.First(k => k.Purpose == Purpose);
            return Convert.FromBase64String(winner.KeyMaterial);
        }
    }
}
