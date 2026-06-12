using CorraStudio.Rendering.Models;

namespace CorraStudio.Rendering.Exporters;

public interface IImageExporter
{
    Task<byte[]> ExportAsync(byte[] imageData, ExportSettings settings);
    Task<string> ExportToFileAsync(byte[] imageData, string outputPath, ExportSettings settings);
    Task<byte[]> ConvertFormatAsync(byte[] imageData, ImageFormat targetFormat, int quality = 90);
}

public class ExportSettings
{
    public ImageFormat Format { get; set; } = ImageFormat.Png;
    public int Quality { get; set; } = 90;
    public bool PreserveMetadata { get; set; } = true;
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
    public bool OptimizeForWeb { get; set; } = false;
}

public class ImageExporter : IImageExporter
{
    private readonly IRenderEngine _renderEngine;

    public ImageExporter(IRenderEngine renderEngine)
    {
        _renderEngine = renderEngine;
    }

    public async Task<byte[]> ExportAsync(byte[] imageData, ExportSettings settings)
    {
        var result = imageData;
        
        // Resize if needed
        if (settings.MaxWidth.HasValue || settings.MaxHeight.HasValue)
        {
            using var original = SKBitmap.Decode(imageData);
            var targetWidth = settings.MaxWidth ?? original.Width;
            var targetHeight = settings.MaxHeight ?? original.Height;
            
            result = await _renderEngine.ResizeAsync(result, targetWidth, targetHeight, true);
        }
        
        // Optimize for web
        if (settings.OptimizeForWeb && settings.Format == ImageFormat.Jpeg)
        {
            settings.Quality = 85;
        }
        
        // Convert format if needed
        if (settings.Format != ImageFormat.Png)
        {
            result = await ConvertFormatAsync(result, settings.Format, settings.Quality);
        }
        
        return result;
    }

    public async Task<string> ExportToFileAsync(byte[] imageData, string outputPath, ExportSettings settings)
    {
        var exported = await ExportAsync(imageData, settings);
        await File.WriteAllBytesAsync(outputPath, exported);
        return outputPath;
    }

    public async Task<byte[]> ConvertFormatAsync(byte[] imageData, ImageFormat targetFormat, int quality = 90)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var image = SKImage.FromBitmap(bitmap);
        
        var encoder = targetFormat switch
        {
            ImageFormat.Png => SKEncodedImageFormat.Png,
            ImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            ImageFormat.Tiff => SKEncodedImageFormat.Tiff,
            ImageFormat.Bmp => SKEncodedImageFormat.Bmp,
            ImageFormat.Webp => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
        
        using var data = image.Encode(encoder, quality);
        return data.ToArray();
    }
}
