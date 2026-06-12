using CorraStudio.Rendering.Models;

namespace CorraStudio.Rendering.RenderEngine;

public interface IRenderEngine
{
    event EventHandler<RenderProgress>? ProgressChanged;
    
    Task<byte[]> RenderImageAsync(byte[] imageData, RenderSettings settings, ImageEffects? effects = null);
    Task<string> RenderImageToFileAsync(byte[] imageData, string outputPath, RenderSettings settings, ImageEffects? effects = null);
    Task<BatchRenderResult> RenderBatchAsync(List<RenderJob> jobs, BatchRenderSettings settings, IProgress<RenderProgress>? progress = null);
    
    Task<byte[]> PrepareForPrintAsync(byte[] imageData, PrintSettings settings);
    Task<byte[]> AddCropMarksAsync(byte[] imageData, PrintSettings settings);
    Task<byte[]> ColorCorrectAsync(byte[] imageData, PrintSettings settings);
    
    Task<byte[]> ApplyEffectsAsync(byte[] imageData, ImageEffects effects);
    Task<byte[]> ResizeAsync(byte[] imageData, int width, int height, bool highQuality = true);
    Task<byte[]> CropAsync(byte[] imageData, SKRect cropRect);
    
    Task<SKEncodedImageFormat> GetEncoderForFormat(ImageFormat format);
    byte[] EncodeImage(SKBitmap bitmap, ImageFormat format, int quality);
}

public interface IPrintRenderEngine : IRenderEngine
{
    Task<byte[]> CreatePrintReadyAsync(byte[] imageData, PrintSettings settings);
    Task<byte[]> AddBleedAsync(byte[] imageData, PrintSettings settings);
    Task<byte[]> CreateColorTestPageAsync(PrintSettings settings);
    Task<byte[]> CreateAlignmentPageAsync(PrintSettings settings);
}

public interface IBatchRenderEngine : IRenderEngine
{
    Task<BatchRenderResult> RenderQueueAsync(ConcurrentQueue<RenderJob> queue, BatchRenderSettings settings);
    Task<bool> PauseBatchAsync();
    Task<bool> ResumeBatchAsync();
    Task<bool> CancelBatchAsync();
}
