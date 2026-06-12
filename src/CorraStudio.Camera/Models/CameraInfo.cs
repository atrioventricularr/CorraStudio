namespace CorraStudio.Camera.Models;

public class CameraInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public CameraType Type { get; set; }
    public bool IsConnected { get; set; }
    public bool IsAvailable { get; set; }
    public Dictionary<string, string> Capabilities { get; set; } = new();
    public List<Resolution> SupportedResolutions { get; set; } = new();
    public Resolution? CurrentResolution { get; set; }
}

public class Resolution
{
    public int Width { get; set; }
    public int Height { get; set; }
    
    public override string ToString() => $"{Width}x{Height}";
}

public enum CameraType
{
    Unknown = 0,
    Webcam = 1,
    Canon = 2,
    Sony = 3,
    Nikon = 4
}
