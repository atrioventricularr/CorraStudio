using CorraStudio.Camera.Implementations.Canon;
using CorraStudio.Camera.Implementations.Sony;
using CorraStudio.Camera.Implementations.Webcam;

namespace CorraStudio.Camera.Services;

public interface ICameraManager
{
    event EventHandler<ICamera>? CameraConnected;
    event EventHandler<string>? CameraDisconnected;
    event EventHandler<CaptureResult>? PhotoCaptured;
    
    Task<List<CameraInfo>> DiscoverAllCamerasAsync();
    Task<ICamera?> ConnectCameraAsync(string cameraId);
    Task DisconnectCameraAsync(string cameraId);
    Task<CaptureResult> CapturePhotoAsync();
    Task<bool> StartLiveViewAsync();
    Task<bool> StopLiveViewAsync();
    ICamera? CurrentCamera { get; }
    bool HasCameraConnected { get; }
}

public class CameraManager : ICameraManager
{
    private readonly WebcamService _webcamService;
    private ICamera? _currentCamera;
    private readonly object _lock = new();

    public event EventHandler<ICamera>? CameraConnected;
    public event EventHandler<string>? CameraDisconnected;
    public event EventHandler<CaptureResult>? PhotoCaptured;

    public ICamera? CurrentCamera => _currentCamera;
    public bool HasCameraConnected => _currentCamera != null && _currentCamera.IsConnected;

    public CameraManager()
    {
        _webcamService = new WebcamService();
        
        _webcamService.CameraConnected += OnCameraConnected;
        _webcamService.CameraDisconnected += OnCameraDisconnected;
    }

    public async Task<List<CameraInfo>> DiscoverAllCamerasAsync()
    {
        var allCameras = new List<CameraInfo>();
        
        // Discover webcams
        var webcams = await _webcamService.DiscoverCamerasAsync();
        allCameras.AddRange(webcams);
        
        // Discover Canon cameras (would need actual device detection)
        // For now, add placeholder
        allCameras.Add(new CameraInfo
        {
            Id = "canon_dummy_1",
            Name = "Canon EOS Series",
            Manufacturer = "Canon",
            Type = CameraType.Canon,
            IsAvailable = true
        });
        
        // Discover Sony cameras
        allCameras.Add(new CameraInfo
        {
            Id = "sony_dummy_1",
            Name = "Sony Alpha Series",
            Manufacturer = "Sony",
            Type = CameraType.Sony,
            IsAvailable = true
        });
        
        return allCameras;
    }

    public async Task<ICamera?> ConnectCameraAsync(string cameraId)
    {
        lock (_lock)
        {
            if (_currentCamera != null)
                return _currentCamera;
        }
        
        ICamera? camera = null;
        
        if (cameraId.Contains("canon", StringComparison.OrdinalIgnoreCase))
        {
            camera = new CanonCamera(cameraId, "Canon EOS");
        }
        else if (cameraId.Contains("sony", StringComparison.OrdinalIgnoreCase))
        {
            camera = new SonyCamera(cameraId, "Sony Alpha");
        }
        else
        {
            camera = await _webcamService.ConnectCameraAsync(cameraId);
        }
        
        if (camera != null)
        {
            var connected = await camera.ConnectAsync();
            if (connected)
            {
                lock (_lock)
                {
                    _currentCamera = camera;
                }
                
                camera.CaptureCompleted += OnCaptureCompleted;
                camera.ErrorOccurred += OnCameraError;
                
                CameraConnected?.Invoke(this, camera);
            }
        }
        
        return camera;
    }

    public async Task DisconnectCameraAsync(string cameraId)
    {
        ICamera? cameraToDisconnect = null;
        
        lock (_lock)
        {
            if (_currentCamera != null && _currentCamera.Info.Id == cameraId)
            {
                cameraToDisconnect = _currentCamera;
                _currentCamera = null;
            }
        }
        
        if (cameraToDisconnect != null)
        {
            cameraToDisconnect.CaptureCompleted -= OnCaptureCompleted;
            cameraToDisconnect.ErrorOccurred -= OnCameraError;
            await cameraToDisconnect.DisconnectAsync();
            CameraDisconnected?.Invoke(this, cameraId);
        }
    }

    public async Task<CaptureResult> CapturePhotoAsync()
    {
        if (_currentCamera == null || !_currentCamera.IsConnected)
            return CaptureResult.FailResult("No camera connected");
        
        var result = await _currentCamera.CaptureAsync();
        if (result.Success)
        {
            PhotoCaptured?.Invoke(this, result);
        }
        return result;
    }

    public async Task<bool> StartLiveViewAsync()
    {
        if (_currentCamera == null || !_currentCamera.IsConnected)
            return false;
        
        _currentCamera.LiveViewFrameReceived += OnLiveViewFrameReceived;
        return await _currentCamera.StartLiveViewAsync();
    }

    public async Task<bool> StopLiveViewAsync()
    {
        if (_currentCamera == null || !_currentCamera.IsConnected)
            return false;
        
        _currentCamera.LiveViewFrameReceived -= OnLiveViewFrameReceived;
        return await _currentCamera.StopLiveViewAsync();
    }

    private void OnCameraConnected(object? sender, CameraInfo info)
    {
        // Handle camera connected
    }

    private void OnCameraDisconnected(object? sender, CameraInfo info)
    {
        // Handle camera disconnected
    }

    private void OnCaptureCompleted(object? sender, CaptureResult result)
    {
        PhotoCaptured?.Invoke(this, result);
    }

    private void OnCameraError(object? sender, string error)
    {
        // Handle camera error
    }

    private void OnLiveViewFrameReceived(object? sender, LiveViewFrame frame)
    {
        // Handle live view frame
    }
}
