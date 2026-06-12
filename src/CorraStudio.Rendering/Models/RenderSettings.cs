namespace CorraStudio.Rendering.Models;

public class RenderSettings
{
    public int Dpi { get; set; } = 300;
    public SKColor BackgroundColor { get; set; } = SKColors.White;
    public bool EnableAntiAlias { get; set; } = true;
    public bool EnableHighQuality { get; set; } = true;
    public ImageFormat OutputFormat { get; set; } = ImageFormat.Png;
    public int Quality { get; set; } = 95;
    public ColorSpace ColorSpace { get; set; } = ColorSpace.sRGB;
    public bool PreserveMetadata { get; set; } = true;
}

public class PrintSettings : RenderSettings
{
    public int BleedSizePixels { get; set; } = 36; // 3mm at 300dpi
    public bool AddCropMarks { get; set; } = true;
    public bool AddColorBars { get; set; } = false;
    public string? PrinterProfile { get; set; }
    public PaperSize PaperSize { get; set; } = PaperSize.Photo4x6;
    public bool EnableColorCorrection { get; set; } = true;
}

public class BatchRenderSettings
{
    public int MaxConcurrentJobs { get; set; } = 4;
    public bool StopOnError { get; set; } = false;
    public string OutputDirectory { get; set; } = string.Empty;
    public NamingConvention NamingConvention { get; set; } = NamingConvention.SessionId_Order;
}

public enum ImageFormat
{
    Png = 0,
    Jpeg = 1,
    Tiff = 2,
    Bmp = 3,
    Webp = 4
}

public enum ColorSpace
{
    sRGB = 0,
    AdobeRGB = 1,
    ProPhotoRGB = 2,
    CMYK = 3
}

public enum PaperSize
{
    Photo2x3 = 0,
    Photo3x4 = 1,
    Photo4x6 = 2,
    Photo5x7 = 3,
    Photo6x8 = 4,
    A4 = 5,
    A5 = 6,
    Letter = 7
}

public enum NamingConvention
{
    SessionId_Order = 0,
    SessionCode_Order = 1,
    Timestamp = 2,
    Custom = 3
}

public static class PaperSizeExtensions
{
    public static (int WidthPixels, int HeightPixels) GetDimensions(this PaperSize size, int dpi = 300)
    {
        return size switch
        {
            PaperSize.Photo2x3 => (2 * dpi, 3 * dpi),
            PaperSize.Photo3x4 => (3 * dpi, 4 * dpi),
            PaperSize.Photo4x6 => (4 * dpi, 6 * dpi),
            PaperSize.Photo5x7 => (5 * dpi, 7 * dpi),
            PaperSize.Photo6x8 => (6 * dpi, 8 * dpi),
            PaperSize.A4 => (2480, 3508),
            PaperSize.A5 => (1748, 2480),
            PaperSize.Letter => (2550, 3300),
            _ => (1800, 1200)
        };
    }
}
