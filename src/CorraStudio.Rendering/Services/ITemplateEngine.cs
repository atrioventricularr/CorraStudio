using CorraStudio.Rendering.Models;

namespace CorraStudio.Rendering.Services;

public interface ITemplateEngine
{
    event EventHandler<RenderingProgressEventArgs>? RenderingProgress;
    
    Task<TemplateModel> LoadTemplateAsync(Guid templateId);
    Task<TemplateModel> LoadTemplateAsync(string templatePath);
    Task<bool> SaveTemplateAsync(TemplateModel template);
    Task<bool> DeleteTemplateAsync(Guid templateId);
    Task<List<TemplateModel>> GetAllTemplatesAsync(Guid tenantId);
    
    Task<LayoutModel> LoadLayoutAsync(Guid layoutId);
    Task<LayoutModel> LoadLayoutAsync(string layoutPath);
    Task<bool> SaveLayoutAsync(LayoutModel layout);
    Task<bool> DeleteLayoutAsync(Guid layoutId);
    Task<List<LayoutModel>> GetAllLayoutsAsync(Guid tenantId);
    
    Task<RenderingResult> RenderTemplateAsync(TemplateModel template, List<byte[]> photos, Dictionary<string, string>? variables = null);
    Task<RenderingResult> RenderLayoutAsync(LayoutModel layout, List<byte[]> photos, Dictionary<string, string>? variables = null);
    Task<RenderingResult> RenderPhotoStripAsync(List<byte[]> photos, TemplateModel? template = null);
    Task<GifRenderingResult> RenderGifAsync(List<byte[]> photos, int frameDelayMs = 500, bool loop = true);
    
    Task<byte[]> GenerateThumbnailAsync(byte[] imageData, int maxWidth = 300, int maxHeight = 300);
    Task<byte[]> ResizeImageAsync(byte[] imageData, int width, int height, bool maintainAspectRatio = true);
    Task<byte[]> ApplyFilterAsync(byte[] imageData, ImageFilter filter);
    Task<byte[]> AddTextOverlayAsync(byte[] imageData, string text, TextPosition position);
}

public enum ImageFilter
{
    None = 0,
    Grayscale = 1,
    Sepia = 2,
    Brightness = 3,
    Contrast = 4,
    Sharpen = 5,
    Blur = 6
}

public enum TextPosition
{
    TopLeft = 0,
    TopCenter = 1,
    TopRight = 2,
    Center = 3,
    BottomLeft = 4,
    BottomCenter = 5,
    BottomRight = 6
}

public class RenderingProgressEventArgs : EventArgs
{
    public int ProgressPercentage { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
