namespace CorraStudio.Rendering.Models;

public class RenderProgress
{
    public Guid JobId { get; set; }
    public int ProgressPercentage { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int ItemsProcessed { get; set; }
    public int TotalItems { get; set; }
}

public class BatchRenderResult
{
    public bool Success { get; set; }
    public List<RenderJobResult> Results { get; set; } = new();
    public TimeSpan TotalTime { get; set; }
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailureCount => Results.Count(r => !r.Success);
}

public class RenderJobResult
{
    public Guid JobId { get; set; }
    public bool Success { get; set; }
    public string? OutputPath { get; set; }
    public byte[]? ImageData { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}

public class RenderJob
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public RenderSettings Settings { get; set; } = new();
    public ImageEffects? Effects { get; set; }
    public string? OutputPath { get; set; }
}
