namespace CorraStudio.Sync.CloudSync;

public interface ICloudSyncService
{
    event EventHandler<SyncProgressEventArgs>? SyncProgress;
    event EventHandler<SyncCompletedEventArgs>? SyncCompleted;
    
    Task<bool> InitializeAsync(CloudSyncConfig config);
    Task<SyncResult> SyncNowAsync();
    Task<SyncResult> SyncEntityAsync(SyncEntityType entityType, string entityId);
    Task<bool> QueueSyncItemAsync(SyncQueueItem item);
    Task<bool> CancelSyncItemAsync(Guid itemId);
    Task<SyncQueueItem?> GetSyncItemAsync(Guid itemId);
    Task<List<SyncQueueItem>> GetPendingSyncItemsAsync();
    Task<SyncStatistics> GetStatisticsAsync(Guid tenantId);
    Task<bool> IsCloudConnectedAsync();
    Task<bool> RegisterDeviceAsync(string deviceName, string deviceId);
    Task<bool> HeartbeatAsync(string deviceId);
    Task<List<CloudDevice>> GetConnectedDevicesAsync(Guid tenantId);
    Task<bool> SendCommandToDeviceAsync(string deviceId, CloudCommand command);
}

public class SyncProgressEventArgs : EventArgs
{
    public int CurrentItem { get; set; }
    public int TotalItems { get; set; }
    public string CurrentEntity { get; set; } = string.Empty;
    public SyncOperation Operation { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SyncCompletedEventArgs : EventArgs
{
    public bool Success { get; set; }
    public int ItemsSynced { get; set; }
    public int FailedItems { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CloudDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string? IpAddress { get; set; }
    public string? Version { get; set; }
}

public class CloudCommand
{
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
    public string CommandType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
