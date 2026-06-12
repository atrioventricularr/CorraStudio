namespace CorraStudio.Camera.Models;

public class CameraSettings
{
    public Resolution? Resolution { get; set; }
    public int Brightness { get; set; } = 0;
    public int Contrast { get; set; } = 0;
    public int Saturation { get; set; } = 0;
    public int Sharpness { get; set; } = 0;
    public bool AutoFocus { get; set; } = true;
    public bool AutoWhiteBalance { get; set; } = true;
    public int Iso { get; set; } = 400;
    public int ExposureCompensation { get; set; } = 0;
    public string? WhiteBalance { get; set; }
    public FlashMode FlashMode { get; set; } = FlashMode.Auto;
    public FocusMode FocusMode { get; set; } = FocusMode.Auto;
}

public enum FlashMode
{
    Off = 0,
    On = 1,
    Auto = 2,
    RedEyeReduction = 3
}

public enum FocusMode
{
    Auto = 0,
    Manual = 1,
    Continuous = 2
}
