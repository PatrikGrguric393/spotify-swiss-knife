namespace spotify_swiss_knife.Models;

public class UserFile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }

    public string? LinkedAlbumId { get; set; }

    public AppUser? User { get; set; }
    public Album? LinkedAlbum { get; set; }
}
