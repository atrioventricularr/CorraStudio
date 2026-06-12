using System.Drawing;
using System.Drawing.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;

namespace CorraStudio.Camera.Implementations.Webcam;

public class WebcamDevice : ICamera
{
    private VideoCaptureDevice? _videoDevice;
    private FilterInfo? _deviceInfo;
    private bool _isConnected;
    private bool _isLiveViewActive;
    private CameraSettings _settings;
    private readonly object _lock = new();

    public event EventHandler<LiveViewFrame>? LiveViewFrameReceived;
    public event EventHandler<CaptureResult>? CaptureCompleted;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<bool>? ConnectionStatusChanged;

    public CameraInfo Info { get; private set; }
    public bool IsConnected => _isConnected;
    public bool IsLiveViewActive => _isLiveViewActive;
    public CameraSettings Settings
    {
        get => _settings;
        set => _settings = value;
    }

    public WebcamDevice(FilterInfo deviceInfo)
    {
        _deviceInfo = deviceInfo;
        _settings = new CameraSettings();
        
        Info = new CameraInfo
        {
            Id = deviceInfo.MonikerString,
            Name = deviceInfo.Name,
            Manufacturer = "Generic",
            Type = CameraType.Webcam,
            IsConnected = false,
            IsAvailable = true
        };
    }

    public async Task<bool> ConnectAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                lock (_lock)
                {
                    if (_videoDevice != null)
                    {
                        _videoDevice.SignalToStop();
                        _videoDevice = null;
                    }

                    _videoDevice = new VideoCaptureDevice(_deviceInfo!.MonikerString);
                    _videoDevice.NewFrame += OnNewFrame;
                    _videoDevice.Start();
                    
                    _isConnected = true;
                    Info.IsConnected = true;
                    ConnectionStatusChanged?.Invoke(this, true);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to connect: {ex.Message}");
                return false;
            }
        });
    }

    public async Task DisconnectAsync()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_videoDevice != null)
                {
                    _videoDevice.NewFrame -= OnNewFrame;
                    _videoDevice.SignalToStop();
                    _videoDevice = null;
                }
                
                _isConnected = false;
                _isLiveViewActive = false;
                Info.IsConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
            }
        });
    }

    public async Task<CaptureResult> CaptureAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (_videoDevice == null || !_isConnected)
                    return CaptureResult.FailResult("Camera not connected");

                var snapshotDevice = new VideoCaptureDevice(_deviceInfo!.MonikerString);
                snapshotDevice.Start();
                
                // Wait for a frame
                var tcs = new TaskCompletionSource<Bitmap>();
                EventHandler<NewFrameEventArgs> handler = null!;
                handler = (s, e) =>
                {
                    if (!tcs.Task.IsCompleted)
                    {
                        var bitmap = (Bitmap)e.Frame.Clone();
                        tcs.SetResult(bitmap);
                        snapshotDevice.NewFrame -= handler;
                    }
                };
                
                snapshotDevice.NewFrame += handler;
                
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                snapshotDevice.SignalToStop();
                
                if (completedTask == timeoutTask)
                    return CaptureResult.FailResult("Capture timeout");
                
                using var bitmap = await tcs.Task;
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Jpeg);
                var imageData = ms.ToArray();
                
                return CaptureResult.SuccessResult(
                    imageData,
                    bitmap.Width,
                    bitmap.Height,
                    imageData.Length
                );
            }
            catch (Exception ex)
            {
                return CaptureResult.FailResult($"Capture failed: {ex.Message}");
            }
        });
    }

    public async Task<CaptureResult> CaptureAsync(string savePath)
    {
        var result = await CaptureAsync();
        if (result.Success && result.ImageData != null)
        {
            await File.WriteAllBytesAsync(savePath, result.ImageData);
            result.FilePath = savePath;
        }
        return result;
    }

    public async Task<bool> StartLiveViewAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                _isLiveViewActive = true;
                return true;
            }
        });
    }

    public async Task<bool> StopLiveViewAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                _isLiveViewActive = false;
                return true;
            }
        });
    }

    public async Task<bool> SetSettingAsync(string settingName, object value)
    {
        return await Task.Run(() =>
        {
            try
            {
                switch (settingName.ToLower())
                {
                    case "brightness":
                        if (_videoDevice != null && value is int brightness)
                            _videoDevice.Brightness = brightness;
                        break;
                    case "contrast":
                        if (_videoDevice != null && value is int contrast)
                            _videoDevice.Contrast = contrast;
                        break;
                    case "saturation":
                        if (_videoDevice != null && value is int saturation)
                            _videoDevice.Saturation = saturation;
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<T?> GetSettingAsync<T>(string settingName)
    {
        return await Task.Run(() =>
        {
            try
            {
                switch (settingName.ToLower())
                {
                    case "brightness":
                        if (_videoDevice != null)
                            return (T)(object)_videoDevice.Brightness;
                        break;
                    case "contrast":
                        if (_videoDevice != null)
                            return (T)(object)_videoDevice.Contrast;
                        break;
                    case "saturation":
                        if (_videoDevice != null)
                            return (T)(object)_videoDevice.Saturation;
                        break;
                }
                return default;
            }
            catch
            {
                return default;
            }
        });
    }

    public async Task<List<Resolution>> GetSupportedResolutionsAsync()
    {
        return await Task.Run(() =>
        {
            var resolutions = new List<Resolution>();
            if (_videoDevice != null)
            {
                foreach (var capability in _videoDevice.VideoCapabilities)
                {
                    resolutions.Add(new Resolution
                    {
                        Width = capability.FrameSize.Width,
                        Height = capability.FrameSize.Height
                    });
                }
            }
            return resolutions;
        });
    }

    public async Task<bool> SetResolutionAsync(Resolution resolution)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (_videoDevice != null)
                {
                    var capability = _videoDevice.VideoCapabilities
                        .FirstOrDefault(c => c.FrameSize.Width == resolution.Width && c.FrameSize.Height == resolution.Height);
                    
                    if (capability != null)
                    {
                        _videoDevice.VideoResolution = capability;
                        Info.CurrentResolution = resolution;
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    private void OnNewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        if (!_isLiveViewActive) return;
        
        using var bitmap = (Bitmap)eventArgs.Frame.Clone();
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Jpeg);
        
        LiveViewFrameReceived?.Invoke(this, new LiveViewFrame
        {
            FrameData = ms.ToArray(),
            Width = bitmap.Width,
            Height = bitmap.Height,
            Timestamp = DateTime.UtcNow
        });
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}
