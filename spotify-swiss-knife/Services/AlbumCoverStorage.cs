namespace spotify_swiss_knife.Services;

// Stores uploaded album-cover images on the local filesystem under "<storage>/album-covers".
// The storage root comes from FileStorage:Path (absolute, or relative to the content root),
// defaulting to an "uploads" folder. Files are saved under a generated GUID name to avoid
// collisions and path-traversal from the original filename; only a known image allowlist is
// accepted. Registered as a singleton — it holds only the resolved path, no per-request state.
public class AlbumCoverStorage
{
    private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp"
    };

    private readonly string _coverPath;

    public AlbumCoverStorage(IConfiguration configuration, IWebHostEnvironment env)
    {
        var configuredPath = configuration["FileStorage:Path"];
        var basePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(env.ContentRootPath, "uploads")
            : Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(env.ContentRootPath, configuredPath);
        _coverPath = Path.Combine(basePath, "album-covers");
    }

    public static bool IsAllowed(IFormFile file) =>
        AllowedExtensions.ContainsKey(Path.GetExtension(file.FileName));

    public static string ResolveContentType(string fileName) =>
        AllowedExtensions.TryGetValue(Path.GetExtension(fileName), out var contentType)
            ? contentType
            : "application/octet-stream";

    public async Task<string> SaveAsync(IFormFile file)
    {
        Directory.CreateDirectory(_coverPath);
        var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(_coverPath, storedName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        return storedName;
    }

    public string? GetPhysicalPath(string storedName)
    {
        var filePath = Path.Combine(_coverPath, storedName);
        return System.IO.File.Exists(filePath) ? filePath : null;
    }

    public void Delete(string? storedName)
    {
        if (string.IsNullOrWhiteSpace(storedName)) return;

        var filePath = Path.Combine(_coverPath, storedName);
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
    }
}
