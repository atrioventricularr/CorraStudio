using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CorraStudio.Sync.CloudSync;

namespace CorraStudio.Infrastructure.BackgroundServices.CloudSync;

public class CloudSyncBackgroundService : BackgroundService
{
    private readonly ICloudSyncService _cloudSyncService;
    private readonly ILogger<CloudSyncBackgroundService> _logger;
    private readonly string _deviceId;
    private readonly PeriodicTimer? _heartbeatTimer;

    public CloudSyncBackgroundService(
        ICloudSyncService cloudSyncService,
        ILogger<CloudSyncBackgroundService> logger)
    {
        _cloudSyncService = cloudSyncService;
        _logger = logger;
        _deviceId = GetDeviceId();
        
        _heartbeatTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cloud Sync Background Service started");
        
        // Register device on startup
        await RegisterDeviceAsync();
        
        // Start heartbeat
        _ = Task.Run(() => SendHeartbeatAsync(stoppingToken), stoppingToken);
        
        // Initial sync
        await _cloudSyncService.SyncNowAsync();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        
        _logger.LogInformation("Cloud Sync Background Service stopped");
    }

    private async Task RegisterDeviceAsync()
    {
        var deviceName = Environment.MachineName;
        var registered = await _cloudSyncService.RegisterDeviceAsync(deviceName, _deviceId);
        
        if (registered)
        {
            _logger.LogInformation("Device {DeviceName} registered successfully", deviceName);
        }
        else
        {
            _logger.LogWarning("Failed to register device {DeviceName}", deviceName);
        }
    }

    private async Task SendHeartbeatAsync(CancellationToken stoppingToken)
    {
        while (await _heartbeatTimer!.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var success = await _cloudSyncService.HeartbeatAsync(_deviceId);
                if (!success)
                {
                    _logger.LogDebug("Heartbeat failed, will retry next cycle");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
            }
        }
    }

    private string GetDeviceId()
    {
        // Generate or retrieve persistent device ID
        var deviceIdPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CorraStudio", "device.id");
        
        if (File.Exists(deviceIdPath))
        {
            return File.ReadAllText(deviceIdPath).Trim();
        }
        
        var deviceId = Guid.NewGuid().ToString();
        var directory = Path.GetDirectoryName(deviceIdPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(deviceIdPath, deviceId);
        
        return deviceId;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cloud Sync Background Service stopping");
        await base.StopAsync(cancellationToken);
    }
}
