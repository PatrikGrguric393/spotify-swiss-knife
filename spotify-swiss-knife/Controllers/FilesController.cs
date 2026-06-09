using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spotify_swiss_knife.DAL;
using spotify_swiss_knife.Models;
using spotify_swiss_knife.Services;

namespace spotify_swiss_knife.Controllers;

[Route("files")]
[Authorize]
public class FilesController : Controller
{
    private readonly SpotifyDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly AlbumCoverStorage _coverStorage;
    private readonly string _uploadPath;

    public FilesController(SpotifyDbContext db, UserManager<AppUser> userManager, AlbumCoverStorage coverStorage, IConfiguration configuration, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _coverStorage = coverStorage;

        var configuredPath = configuration["FileStorage:Path"];
        _uploadPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(env.ContentRootPath, "uploads")
            : Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(env.ContentRootPath, configuredPath);
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var userId = _userManager.GetUserId(User)!;
        var files = await _db.UserFiles
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new
            {
                f.Id,
                name = f.OriginalFileName,
                size = f.FileSize,
                f.ContentType,
                f.UploadedAt,
                linkedAlbumId = f.LinkedAlbumId,
                linkedAlbumName = f.LinkedAlbum != null ? f.LinkedAlbum.Name : null
            })
            .ToListAsync();

        return Json(files);
    }

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var userId = _userManager.GetUserId(User)!;
        var ext = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var userDir = Path.Combine(_uploadPath, userId);
        Directory.CreateDirectory(userDir);
        var filePath = Path.Combine(userDir, storedName);

        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream);

        var userFile = new UserFile
        {
            UserId = userId,
            OriginalFileName = file.FileName,
            StoredFileName = storedName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        _db.UserFiles.Add(userFile);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            userFile.Id,
            userFile.OriginalFileName,
            userFile.FileSize,
            userFile.ContentType,
            userFile.UploadedAt
        });
    }

    [HttpGet("download/{id:int}")]
    public async Task<IActionResult> Download(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var file = await _db.UserFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        if (file is null)
            return NotFound();

        string filePath;
        if (file.LinkedAlbumId is not null)
        {
            filePath = _coverStorage.GetPhysicalPath(file.StoredFileName) ?? string.Empty;
        }
        else
        {
            filePath = Path.Combine(_uploadPath, userId, file.StoredFileName);
        }

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        return PhysicalFile(filePath, file.ContentType, file.OriginalFileName);
    }

    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var file = await _db.UserFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        if (file is null)
            return NotFound();

        if (file.LinkedAlbumId is not null)
        {
            var album = await _db.Albums.FirstOrDefaultAsync(a => a.Id == file.LinkedAlbumId);
            if (album is not null)
            {
                album.CoverImageFileName = null;
                album.CoverImageContentType = null;
            }
            _coverStorage.Delete(file.StoredFileName);
        }
        else
        {
            var filePath = Path.Combine(_uploadPath, userId, file.StoredFileName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        _db.UserFiles.Remove(file);
        await _db.SaveChangesAsync();

        return Ok();
    }
}
