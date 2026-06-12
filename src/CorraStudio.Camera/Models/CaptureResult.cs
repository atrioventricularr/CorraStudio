namespace CorraStudio.Camera.Models;

public class CaptureResult
{
    public bool Success { get; set; }
    public byte[]? ImageData { get; set; }
    public string? FilePath { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CapturedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static CaptureResult SuccessResult(byte[] imageData, int width, int height, long fileSize)
    {
        return new CaptureResult
        {
            Success = true,
            ImageData = imageData,
            Width = width,
            Height = height,
            FileSizeBytes = fileSize,
            CapturedAt = DateTime.UtcNow
        };
    }

    public static CaptureResult FailResult(string errorMessage)
    {
        return new CaptureResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            CapturedAt = DateTime.UtcNow
        };
    }
}

public class LiveViewFrame
{
    public byte[]? FrameData { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime Timestamp { get; set; }
}
