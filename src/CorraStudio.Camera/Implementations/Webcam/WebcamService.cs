using AForge.Video.DirectShow;

namespace CorraStudio.Camera.Implementations.Webcam;

public class WebcamService : ICameraService
{
    private readonly Dictionary<string, WebcamDevice> _activeCameras = new();
    private readonly object _lock = new();

    public event EventHandler<CameraInfo>? CameraConnected;
    public event EventHandler<CameraInfo>? CameraDisconnected;

    public async Task<List<CameraInfo>> DiscoverCamerasAsync()
    {
        return await Task.Run(() =>
        {
            var cameras = new List<CameraInfo>();
            
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            
            foreach (FilterInfo device in videoDevices)
            {
                cameras.Add(new CameraInfo
                {
                    Id = device.MonikerString,
                    Name = device.Name,
                    Manufacturer = "Generic",
                    Type = CameraType.Webcam,
                    IsConnected = false,
                    IsAvailable = true
                });
            }
            
            return cameras;
        });
    }

    public async Task<ICamera?> ConnectCameraAsync(string cameraId)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_activeCameras.ContainsKey(cameraId))
                    return _activeCameras[cameraId];
                
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                FilterInfo? deviceInfo = null;
                
                foreach (FilterInfo device in videoDevices)
                {
                    if (device.MonikerString == cameraId)
                    {
                        deviceInfo = device;
                        break;
                    }
                }
                
                if (deviceInfo == null)
                    return null;
                
                var camera = new WebcamDevice(deviceInfo);
                _activeCameras[cameraId] = camera;
                CameraConnected?.Invoke(this, camera.Info);
                
                return camera;
            }
        });
    }

    public async Task DisconnectCameraAsync(string cameraId)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_activeCameras.TryGetValue(cameraId, out var camera))
                {
                    camera.DisconnectAsync().Wait();
                    camera.Dispose();
                    _activeCameras.Remove(cameraId);
                    CameraDisconnected?.Invoke(this, camera.Info);
                }
            }
        });
    }

    public ICamera? GetActiveCamera()
    {
        lock (_lock)
        {
            return _activeCameras.Values.FirstOrDefault();
        }
    }

    public async Task<bool> IsCameraAvailableAsync(string cameraId)
    {
        return await Task.Run(() =>
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in videoDevices)
            {
                if (device.MonikerString == cameraId)
                    return true;
            }
            return false;
        });
    }
}
