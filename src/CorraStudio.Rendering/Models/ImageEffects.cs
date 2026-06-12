namespace CorraStudio.Rendering.Models;

public class ImageEffects
{
    public VignetteSettings? Vignette { get; set; }
    public ShadowSettings? Shadow { get; set; }
    public BorderSettings? Border { get; set; }
    public CornerSettings? CornerRadius { get; set; }
    public WatermarkSettings? Watermark { get; set; }
    public ColorAdjustment? ColorAdjustment { get; set; }
    public SharpenSettings? Sharpen { get; set; }
    public BlurSettings? Blur { get; set; }
}

public class VignetteSettings
{
    public double Intensity { get; set; } = 0.5;
    public SKColor Color { get; set; } = SKColors.Black;
    public VignetteShape Shape { get; set; } = VignetteShape.Circle;
}

public enum VignetteShape
{
    Circle = 0,
    Rectangle = 1,
    RoundedRectangle = 2
}

public class ShadowSettings
{
    public int OffsetX { get; set; } = 5;
    public int OffsetY { get; set; } = 5;
    public double BlurRadius { get; set; } = 10;
    public SKColor Color { get; set; } = new SKColor(0, 0, 0, 100);
    public double Opacity { get; set; } = 0.5;
}

public class BorderSettings
{
    public int Width { get; set; } = 2;
    public SKColor Color { get; set; } = SKColors.Black;
    public BorderStyle Style { get; set; } = BorderStyle.Solid;
}

public enum BorderStyle
{
    Solid = 0,
    Dashed = 1,
    Dotted = 2,
    Double = 3
}

public class CornerSettings
{
    public double Radius { get; set; } = 10;
    public CornerType Type { get; set; } = CornerType.All;
}

public enum CornerType
{
    All = 0,
    TopLeft = 1,
    TopRight = 2,
    BottomLeft = 3,
    BottomRight = 4
}

public class WatermarkSettings
{
    public string Text { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public WatermarkPosition Position { get; set; } = WatermarkPosition.BottomRight;
    public double Opacity { get; set; } = 0.5;
    public int Margin { get; set; } = 10;
}

public enum WatermarkPosition
{
    TopLeft = 0,
    TopCenter = 1,
    TopRight = 2,
    Center = 3,
    BottomLeft = 4,
    BottomCenter = 5,
    BottomRight = 6
}

public class ColorAdjustment
{
    public float Brightness { get; set; } = 0;
    public float Contrast { get; set; } = 0;
    public float Saturation { get; set; } = 0;
    public float Hue { get; set; } = 0;
    public float Gamma { get; set; } = 1.0f;
}

public class SharpenSettings
{
    public double Amount { get; set; } = 1.0;
    public double Radius { get; set; } = 1.0;
}

public class BlurSettings
{
    public double Radius { get; set; } = 5.0;
    public BlurType Type { get; set; } = BlurType.Gaussian;
}

public enum BlurType
{
    Gaussian = 0,
    Box = 1,
    Motion = 2
}
