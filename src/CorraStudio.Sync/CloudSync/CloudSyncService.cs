using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace CorraStudio.Sync.CloudSync;

public class CloudSyncService : ICloudSyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentQueue<SyncQueueItem> _syncQueue;
    private readonly ConcurrentDictionary<Guid, SyncQueueItem> _activeItems;
    private CloudSyncConfig _config;
    private HttpClient? _httpClient;
    private Timer? _syncTimer;
    private bool _isSyncing;
    private bool _isInitialized;
    private readonly ILogger<CloudSyncService>? _logger;

    public event EventHandler<SyncProgressEventArgs>? SyncProgress;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public CloudSyncService(IServiceProvider serviceProvider, ILogger<CloudSyncService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _syncQueue = new ConcurrentQueue<SyncQueueItem>();
        _activeItems = new ConcurrentDictionary<Guid, SyncQueueItem>();
        _config = new CloudSyncConfig();
    }

    public async Task<bool> InitializeAsync(CloudSyncConfig config)
    {
        _config = config;
        
        if (string.IsNullOrEmpty(_config.CloudApiUrl))
        {
            _logger?.LogWarning("Cloud API URL not configured");
            return false;
        }
        
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.CloudApiKey ?? "");
        _httpClient.DefaultRequestHeaders.Add("X-Client-Type", "WPF-Photobooth");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // Test connection
        var isConnected = await IsCloudConnectedAsync();
        
        if (isConnected && _config.SyncOnStartup)
        {
            _ = Task.Run(async () => await SyncNowAsync());
        }
        
        if (_config.EnableAutoSync)
        {
            _syncTimer = new Timer(async _ => await SyncNowAsync(), null, 
                TimeSpan.FromSeconds(_config.SyncIntervalSeconds), 
                TimeSpan.FromSeconds(_config.SyncIntervalSeconds));
        }
        
        _isInitialized = true;
        _logger?.LogInformation("Cloud sync service initialized");
        
        return isConnected;
    }

    public async Task<SyncResult> SyncNowAsync()
    {
        if (_isSyncing)
            return new SyncResult { Success = false, ErrorMessage = "Sync already in progress" };
        
        _isSyncing = true;
        var startTime = DateTime.UtcNow;
        var syncedCount = 0;
        var failedCount = 0;
        
        try
        {
            var pendingItems = await GetPendingSyncItemsAsync();
            var totalItems = pendingItems.Count;
            
            var progress = new SyncProgressEventArgs { TotalItems = totalItems };
            
            for (int i = 0; i < pendingItems.Count; i++)
            {
                var item = pendingItems[i];
                progress.CurrentItem = i + 1;
                progress.CurrentEntity = item.EntityType.ToString();
                progress.Operation = item.Operation;
                SyncProgress?.Invoke(this, progress);
                
                var result = await ProcessSyncItemAsync(item);
                
                if (result)
                {
                    syncedCount++;
                }
                else
                {
                    failedCount++;
                }
                
                // Respect batch size
                if (i % _config.BatchSize == 0 && i > 0)
                {
                    await Task.Delay(100);
                }
            }
            
            var duration = DateTime.UtcNow - startTime;
            
            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs
            {
                Success = failedCount == 0,
                ItemsSynced = syncedCount,
                FailedItems = failedCount,
                Duration = duration
            });
            
            return new SyncResult
            {
                Success = true,
                ItemsSynced = syncedCount,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Sync failed");
            return new SyncResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ItemsSynced = syncedCount,
                Duration = DateTime.UtcNow - startTime
            };
        }
        finally
        {
            _isSyncing = false;
        }
    }

    public async Task<SyncResult> SyncEntityAsync(SyncEntityType entityType, string entityId)
    {
        var item = new SyncQueueItem
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Operation = SyncOperation.Update,
            CreatedAt = DateTime.UtcNow,
            Status = SyncStatus.Pending
        };
        
        await QueueSyncItemAsync(item);
        return await SyncNowAsync();
    }

    public async Task<bool> QueueSyncItemAsync(SyncQueueItem item)
    {
        return await Task.Run(() =>
        {
            try
            {
                _syncQueue.Enqueue(item);
                _activeItems.TryAdd(item.Id, item);
                _logger?.LogDebug("Queued sync item {ItemId} for {EntityType}", item.Id, item.EntityType);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to queue sync item");
                return false;
            }
        });
    }

    public async Task<bool> CancelSyncItemAsync(Guid itemId)
    {
        return await Task.Run(() =>
        {
            if (_activeItems.TryGetValue(itemId, out var item))
            {
                item.Status = SyncStatus.Cancelled;
                _activeItems.TryRemove(itemId, out _);
                return true;
            }
            return false;
        });
    }

    public async Task<SyncQueueItem?> GetSyncItemAsync(Guid itemId)
    {
        return await Task.Run(() =>
        {
            _activeItems.TryGetValue(itemId, out var item);
            return item;
        });
    }

    public async Task<List<SyncQueueItem>> GetPendingSyncItemsAsync()
    {
        return await Task.Run(() =>
        {
            return _activeItems.Values
                .Where(i => i.Status == SyncStatus.Pending || i.Status == SyncStatus.Failed)
                .OrderBy(i => i.Priority)
                .ThenBy(i => i.CreatedAt)
                .ToList();
        });
    }

    public async Task<SyncStatistics> GetStatisticsAsync(Guid tenantId)
    {
        return await Task.Run(() =>
        {
            var items = _activeItems.Values;
            var now = DateTime.UtcNow;
            
            return new SyncStatistics
            {
                PendingItems = items.Count(i => i.Status == SyncStatus.Pending),
                ProcessingItems = items.Count(i => i.Status == SyncStatus.Processing),
                CompletedToday = items.Count(i => i.Status == SyncStatus.Completed && i.CompletedAt?.Date == now.Date),
                FailedItems = items.Count(i => i.Status == SyncStatus.Failed),
                LastSyncTime = items.Max(i => i.CompletedAt) ?? DateTime.MinValue,
                IsCloudConnected = _httpClient != null,
                ItemsByEntity = items
                    .GroupBy(i => i.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        });
    }

    public async Task<bool> IsCloudConnectedAsync()
    {
        if (_httpClient == null) return false;
        
        try
        {
            var response = await _httpClient.GetAsync($"{_config.CloudApiUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RegisterDeviceAsync(string deviceName, string deviceId)
    {
        try
        {
            var deviceInfo = new
            {
                device_id = deviceId,
                device_name = deviceName,
                device_type = "WPF-Photobooth",
                version = "1.0.0",
                registered_at = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(deviceInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient!.PostAsync($"{_config.CloudApiUrl}/devices/register", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Device registration failed");
            return false;
        }
    }

    public async Task<bool> HeartbeatAsync(string deviceId)
    {
        try
        {
            var heartbeat = new
            {
                device_id = deviceId,
                timestamp = DateTime.UtcNow,
                status = "online"
            };
            
            var json = JsonSerializer.Serialize(heartbeat);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient!.PostAsync($"{_config.CloudApiUrl}/devices/heartbeat", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<CloudDevice>> GetConnectedDevicesAsync(Guid tenantId)
    {
        try
        {
            var response = await _httpClient!.GetAsync($"{_config.CloudApiUrl}/devices?tenantId={tenantId}");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<CloudDevice>>(json) ?? new List<CloudDevice>();
        }
        catch
        {
            return new List<CloudDevice>();
        }
    }

    public async Task<bool> SendCommandToDeviceAsync(string deviceId, CloudCommand command)
    {
        try
        {
            var json = JsonSerializer.Serialize(command);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient!.PostAsync($"{_config.CloudApiUrl}/devices/{deviceId}/command", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #region Private Methods

    private async Task<bool> ProcessSyncItemAsync(SyncQueueItem item)
    {
        if (_httpClient == null) return false;
        
        item.Status = SyncStatus.Processing;
        item.LastAttemptAt = DateTime.UtcNow;
        
        try
        {
            var payload = new
            {
                item_id = item.Id,
                entity_type = item.EntityType.ToString(),
                operation = item.Operation.ToString(),
                entity_id = item.EntityId,
                data = item.Payload,
                timestamp = item.CreatedAt
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_config.CloudApiUrl}/sync", content);
            
            if (response.IsSuccessStatusCode)
            {
                item.Status = SyncStatus.Completed;
                item.CompletedAt = DateTime.UtcNow;
                _activeItems.TryRemove(item.Id, out _);
                return true;
            }
            
            item.RetryCount++;
            
            if (item.RetryCount >= item.MaxRetries)
            {
                item.Status = SyncStatus.Failed;
                item.ErrorMessage = await response.Content.ReadAsStringAsync();
                _logger?.LogWarning("Sync item {ItemId} failed after {Retries} retries", item.Id, item.RetryCount);
                return false;
            }
            
            // Requeue for retry
            item.Status = SyncStatus.Pending;
            _syncQueue.Enqueue(item);
            return false;
        }
        catch (Exception ex)
        {
            item.RetryCount++;
            item.ErrorMessage = ex.Message;
            
            if (item.RetryCount >= item.MaxRetries)
            {
                item.Status = SyncStatus.Failed;
                return false;
            }
            
            item.Status = SyncStatus.Pending;
            _syncQueue.Enqueue(item);
            return false;
        }
    }

    #endregion
}
