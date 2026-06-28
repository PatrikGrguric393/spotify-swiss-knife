namespace spotify_swiss_knife.Configuration;

public class SpotifyOptions
{
    public const string Section = "Spotify";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "spotify-swiss-knife";
    public string Audience { get; set; } = "spotify-swiss-knife-api";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}

public class FileStorageOptions
{
    public const string Section = "FileStorage";
    public string Path { get; set; } = "uploads";
}

public class SeedAdminOptions
{
    public const string Section = "SeedAdmin";
    public string Email { get; set; } = "admin@ssk.local";
    public string Password { get; set; } = "Admin123!";
}
