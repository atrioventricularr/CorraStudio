namespace CorraStudio.Rendering.Models;

public class RenderingResult
{
    public bool Success { get; set; }
    public byte[]? ImageData { get; set; }
    public string? OutputPath { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static RenderingResult SuccessResult(byte[] imageData, int width, int height)
    {
        return new RenderingResult
        {
            Success = true,
            ImageData = imageData,
            Width = width,
            Height = height,
            FileSizeBytes = imageData.Length,
            ProcessingTime = TimeSpan.Zero
        };
    }

    public static RenderingResult FailResult(string errorMessage)
    {
        return new RenderingResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public class GifRenderingResult : RenderingResult
{
    public int FrameCount { get; set; }
    public int FrameDelayMs { get; set; }
    public bool IsLooping { get; set; } = true;
}
