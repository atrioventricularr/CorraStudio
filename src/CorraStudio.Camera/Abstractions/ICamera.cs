namespace CorraStudio.Camera.Abstractions;

public interface ICamera : IDisposable
{
    event EventHandler<LiveViewFrame>? LiveViewFrameReceived;
    event EventHandler<CaptureResult>? CaptureCompleted;
    event EventHandler<string>? ErrorOccurred;
    event EventHandler<bool>? ConnectionStatusChanged;

    CameraInfo Info { get; }
    bool IsConnected { get; }
    bool IsLiveViewActive { get; }
    CameraSettings Settings { get; set; }

    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<CaptureResult> CaptureAsync();
    Task<CaptureResult> CaptureAsync(string savePath);
    Task<bool> StartLiveViewAsync();
    Task<bool> StopLiveViewAsync();
    Task<bool> SetSettingAsync(string settingName, object value);
    Task<T?> GetSettingAsync<T>(string settingName);
    Task<List<Resolution>> GetSupportedResolutionsAsync();
    Task<bool> SetResolutionAsync(Resolution resolution);
}

public interface ICameraService
{
    event EventHandler<CameraInfo>? CameraConnected;
    event EventHandler<CameraInfo>? CameraDisconnected;
    
    Task<List<CameraInfo>> DiscoverCamerasAsync();
    Task<ICamera?> ConnectCameraAsync(string cameraId);
    Task DisconnectCameraAsync(string cameraId);
    ICamera? GetActiveCamera();
    Task<bool> IsCameraAvailableAsync(string cameraId);
}
