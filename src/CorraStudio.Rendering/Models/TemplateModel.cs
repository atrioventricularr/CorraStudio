using System.Text.Json.Serialization;

namespace CorraStudio.Rendering.Models;

public class TemplateModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TemplateType Type { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Dpi { get; set; } = 300;
    public List<TemplateLayer> Layers { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? PreviewImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum TemplateType
{
    PhotoStrip = 0,
    SinglePhoto = 1,
    Collage = 2,
    GreetingCard = 3,
    Calendar = 4,
    Custom = 5
}

public class TemplateLayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public LayerType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public Rectangle Bounds { get; set; } = new();
    public int ZIndex { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsLocked { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public enum LayerType
{
    Photo = 0,
    Text = 1,
    Shape = 2,
    Image = 3,
    Frame = 4,
    Background = 5,
    Overlay = 6,
    QrCode = 7,
    Date = 8
}

public class Rectangle
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Rotation { get; set; }
    public double Opacity { get; set; } = 1.0;
}

public class TextLayerProperties
{
    public string Text { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Arial";
    public double FontSize { get; set; } = 12;
    public string FontColor { get; set; } = "#000000";
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public TextAlignment Alignment { get; set; } = TextAlignment.Center;
}

public enum TextAlignment
{
    Left = 0,
    Center = 1,
    Right = 2
}

public class PhotoLayerProperties
{
    public PhotoFitMode FitMode { get; set; } = PhotoFitMode.Cover;
    public double CornerRadius { get; set; }
    public string? BorderColor { get; set; }
    public double BorderWidth { get; set; }
    public bool IsCircular { get; set; }
}

public enum PhotoFitMode
{
    Fill = 0,
    Fit = 1,
    Cover = 2,
    Stretch = 3
}
