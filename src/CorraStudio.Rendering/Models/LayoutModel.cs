namespace CorraStudio.Rendering.Models;

public class LayoutModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public List<PhotoSlot> PhotoSlots { get; set; } = new();
    public List<DesignElement> DesignElements { get; set; } = new();
    public LayoutOrientation Orientation { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
}

public class PhotoSlot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int OrderIndex { get; set; }
    public Rectangle Bounds { get; set; } = new();
    public PhotoFitMode FitMode { get; set; } = PhotoFitMode.Cover;
    public double CornerRadius { get; set; }
    public string? BorderColor { get; set; }
    public double BorderWidth { get; set; }
}

public class DesignElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ElementType Type { get; set; }
    public Rectangle Bounds { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

public enum ElementType
{
    Text = 0,
    Shape = 1,
    Decoration = 2,
    Logo = 3,
    Background = 4
}

public enum LayoutOrientation
{
    Portrait = 0,
    Landscape = 1,
    Square = 2
}
