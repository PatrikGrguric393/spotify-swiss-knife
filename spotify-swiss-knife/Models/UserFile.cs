namespace spotify_swiss_knife.Models;

/// <summary>
/// Metadata for a file a user uploaded. The bytes live on disk under <see cref="StoredFileName"/>;
/// only this descriptor is stored in the database. May optionally be linked to an album.
/// </summary>
public class UserFile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Name as the user uploaded it, shown in the UI.
    public string OriginalFileName { get; set; } = string.Empty;

    // Server-generated unique name used on disk, to avoid collisions and path traversal.
    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }

    public string? LinkedAlbumId { get; set; }

    public AppUser? User { get; set; }
    public Album? LinkedAlbum { get; set; }
}
