namespace CorraStudio.Application.DTOs;

public class PhotoDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsSelected { get; set; }
    public DateTime CapturedAt { get; set; }
}

public class CapturePhotoDto
{
    public Guid SessionId { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public int OrderIndex { get; set; }
}

public class SelectPhotosDto
{
    public Guid SessionId { get; set; }
    public List<Guid> PhotoIds { get; set; } = new();
}
