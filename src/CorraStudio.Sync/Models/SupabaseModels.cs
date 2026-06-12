namespace CorraStudio.Sync.Models;

public class GallerySession
{
    public Guid Id { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int PhotoCount { get; set; }
    public bool IsPublic { get; set; } = false;
    public string? PasswordHash { get; set; }
}

public class GalleryPhoto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsDownloadable { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class UploadResult
{
    public bool Success { get; set; }
    public string? PublicUrl { get; set; }
    public string? StoragePath { get; set; }
    public string? ErrorMessage { get; set; }
}

public class GalleryTokenResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? GalleryUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
