using System.Windows.Input;
using System.Windows.Media.Imaging;
using CorraStudio.Camera.Services;
using CorraStudio.Camera.Models;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class CameraViewModel : ViewModelBase
{
    private readonly ICameraManager _cameraManager;
    private BitmapImage? _livePreview;
    private bool _isCameraConnected;
    private string _cameraStatus = "No camera connected";
    private List<CameraInfo> _availableCameras = new();
    private CameraInfo? _selectedCamera;
    private bool _isCapturing;

    public CameraViewModel(ICameraManager cameraManager)
    {
        _cameraManager = cameraManager;
        
        ConnectCommand = new RelayCommand(async () => await ConnectCameraAsync(), () => SelectedCamera != null && !IsCameraConnected);
        DisconnectCommand = new RelayCommand(async () => await DisconnectCameraAsync(), () => IsCameraConnected);
        CaptureCommand = new RelayCommand(async () => await CapturePhotoAsync(), () => IsCameraConnected && !IsCapturing);
        RefreshCamerasCommand = new RelayCommand(async () => await RefreshCamerasAsync());
        
        _cameraManager.PhotoCaptured += OnPhotoCaptured;
        _cameraManager.CameraConnected += OnCameraConnected;
        _cameraManager.CameraDisconnected += OnCameraDisconnected;
        
        // Initialize
        Task.Run(async () => await RefreshCamerasAsync());
    }

    public BitmapImage? LivePreview
    {
        get => _livePreview;
        set => SetField(ref _livePreview, value);
    }

    public bool IsCameraConnected
    {
        get => _isCameraConnected;
        set => SetField(ref _isCameraConnected, value);
    }

    public string CameraStatus
    {
        get => _cameraStatus;
        set => SetField(ref _cameraStatus, value);
    }

    public List<CameraInfo> AvailableCameras
    {
        get => _availableCameras;
        set => SetField(ref _availableCameras, value);
    }

    public CameraInfo? SelectedCamera
    {
        get => _selectedCamera;
        set => SetField(ref _selectedCamera, value);
    }

    public bool IsCapturing
    {
        get => _isCapturing;
        set => SetField(ref _isCapturing, value);
    }

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand CaptureCommand { get; }
    public ICommand RefreshCamerasCommand { get; }

    public event EventHandler<byte[]>? PhotoCaptured;

    private async Task RefreshCamerasAsync()
    {
        try
        {
            IsLoading = true;
            var cameras = await _cameraManager.DiscoverAllCamerasAsync();
            AvailableCameras = cameras;
            CameraStatus = $"Found {cameras.Count} camera(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to discover cameras: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ConnectCameraAsync()
    {
        if (SelectedCamera == null) return;
        
        try
        {
            IsLoading = true;
            var camera = await _cameraManager.ConnectCameraAsync(SelectedCamera.Id);
            
            if (camera != null)
            {
                IsCameraConnected = true;
                CameraStatus = $"Connected to {SelectedCamera.Name}";
                
                // Start live view
                await _cameraManager.StartLiveViewAsync();
            }
            else
            {
                CameraStatus = "Failed to connect to camera";
            }
        }
        catch (Exception ex)
        {
            SetError($"Connection failed: {ex.Message}");
            CameraStatus = "Connection failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DisconnectCameraAsync()
    {
        try
        {
            IsLoading = true;
            await _cameraManager.StopLiveViewAsync();
            
            if (SelectedCamera != null)
            {
                await _cameraManager.DisconnectCameraAsync(SelectedCamera.Id);
            }
            
            IsCameraConnected = false;
            CameraStatus = "Disconnected";
            LivePreview = null;
        }
        catch (Exception ex)
        {
            SetError($"Disconnection failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CapturePhotoAsync()
    {
        try
        {
            IsCapturing = true;
            CameraStatus = "Capturing...";
            
            var result = await _cameraManager.CapturePhotoAsync();
            
            if (result.Success && result.ImageData != null)
            {
                CameraStatus = "Photo captured!";
                PhotoCaptured?.Invoke(this, result.ImageData);
            }
            else
            {
                CameraStatus = $"Capture failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            SetError($"Capture failed: {ex.Message}");
            CameraStatus = "Capture failed";
        }
        finally
        {
            IsCapturing = false;
            await Task.Delay(1000);
            CameraStatus = "Ready";
        }
    }

    private void OnPhotoCaptured(object? sender, CaptureResult result)
    {
        if (result.Success && result.ImageData != null)
        {
            PhotoCaptured?.Invoke(this, result.ImageData);
        }
    }

    private void OnCameraConnected(object? sender, ICamera camera)
    {
        IsCameraConnected = true;
        CameraStatus = $"Connected to {camera.Info.Name}";
    }

    private void OnCameraDisconnected(object? sender, string cameraId)
    {
        IsCameraConnected = false;
        CameraStatus = "Camera disconnected";
        LivePreview = null;
    }

    public override void OnNavigatedFrom()
    {
        base.OnNavigatedFrom();
        Task.Run(async () => await DisconnectCameraAsync());
    }
}
