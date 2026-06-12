namespace CorraStudio.Sync.Queue;

public interface ISyncQueueManager
{
    Task<bool> EnqueueSessionAsync(Session session, List<Photo> photos);
    Task<bool> EnqueuePaymentAsync(PaymentTransaction payment);
    Task<bool> EnqueuePrintJobAsync(PrintJob printJob);
    Task<bool> EnqueueConfigurationChangeAsync(string configKey, string oldValue, string newValue);
    Task<int> GetQueueLengthAsync();
    Task<List<SyncQueueItemSummary>> GetQueueItemsAsync();
    Task<bool> RetryFailedItemsAsync();
    Task<bool> ClearQueueAsync();
}

public class SyncQueueItemSummary
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SyncQueueManager : ISyncQueueManager
{
    private readonly ICloudSyncService _cloudSyncService;
    private readonly ILogger<SyncQueueManager>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SyncQueueManager(ICloudSyncService cloudSyncService, ILogger<SyncQueueManager>? logger = null)
    {
        _cloudSyncService = cloudSyncService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<bool> EnqueueSessionAsync(Session session, List<Photo> photos)
    {
        var payload = new
        {
            session = new
            {
                session.Id,
                session.SessionCode,
                session.Status,
                session.PhotoCount,
                session.CreatedAt,
                session.StartedAt,
                session.CompletedAt,
                session.TotalAmount
            },
            photos = photos.Select(p => new { p.Id, p.FilePath, p.OrderIndex, p.CapturedAt })
        };
        
        var item = new SyncQueueItem
        {
            Id = Guid.NewGuid(),
            TenantId = session.TenantId ?? Guid.Empty,
            EntityType = SyncEntityType.Session,
            Operation = SyncOperation.Create,
            EntityId = session.Id.ToString(),
            Payload = JsonSerializer.Serialize(payload, _jsonOptions),
            CreatedAt = DateTime.UtcNow,
            Status = SyncStatus.Pending,
            Priority = 1
        };
        
        return await _cloudSyncService.QueueSyncItemAsync(item);
    }

    public async Task<bool> EnqueuePaymentAsync(PaymentTransaction payment)
    {
        var payload = new
        {
            payment.Id,
            payment.TransactionCode,
            payment.Amount,
            payment.Method,
            payment.Status,
            payment.PaidAt,
            payment.SessionId
        };
        
        var item = new SyncQueueItem
        {
            Id = Guid.NewGuid(),
            TenantId = payment.TenantId ?? Guid.Empty,
            EntityType = SyncEntityType.Payment,
            Operation = SyncOperation.Create,
            EntityId = payment.Id.ToString(),
            Payload = JsonSerializer.Serialize(payload, _jsonOptions),
            CreatedAt = DateTime.UtcNow,
            Status = SyncStatus.Pending,
            Priority = 2
        };
        
        return await _cloudSyncService.QueueSyncItemAsync(item);
    }

    public async Task<bool> EnqueuePrintJobAsync(PrintJob printJob)
    {
        var payload = new
        {
            printJob.Id,
            printJob.SessionId,
            printJob.PhotoId,
            printJob.PrinterName,
            printJob.CopyCount,
            printJob.Status,
            printJob.PrintedAt
        };
        
        var item = new SyncQueueItem
        {
            Id = Guid.NewGuid(),
            TenantId = printJob.TenantId ?? Guid.Empty,
            EntityType = SyncEntityType.PrintJob,
            Operation = SyncOperation.Create,
            EntityId = printJob.Id.ToString(),
            Payload = JsonSerializer.Serialize(payload, _jsonOptions),
            CreatedAt = DateTime.UtcNow,
            Status = SyncStatus.Pending,
            Priority = 3
        };
        
        return await _cloudSyncService.QueueSyncItemAsync(item);
    }

    public async Task<bool> EnqueueConfigurationChangeAsync(string configKey, string oldValue, string newValue)
    {
        var payload = new
        {
            configKey,
            oldValue,
            newValue,
            changedAt = DateTime.UtcNow
        };
        
        var item = new SyncQueueItem
        {
            Id = Guid.NewGuid(),
            EntityType = SyncEntityType.Configuration,
            Operation = SyncOperation.Update,
            EntityId = configKey,
            Payload = JsonSerializer.Serialize(payload, _jsonOptions),
            CreatedAt = DateTime.UtcNow,
            Status = SyncStatus.Pending,
            Priority = 4
        };
        
        return await _cloudSyncService.QueueSyncItemAsync(item);
    }

    public async Task<int> GetQueueLengthAsync()
    {
        var items = await _cloudSyncService.GetPendingSyncItemsAsync();
        return items.Count;
    }

    public async Task<List<SyncQueueItemSummary>> GetQueueItemsAsync()
    {
        var items = await _cloudSyncService.GetPendingSyncItemsAsync();
        
        return items.Select(i => new SyncQueueItemSummary
        {
            Id = i.Id,
            EntityType = i.EntityType.ToString(),
            Operation = i.Operation.ToString(),
            Status = i.Status.ToString(),
            CreatedAt = i.CreatedAt,
            RetryCount = i.RetryCount,
            ErrorMessage = i.ErrorMessage
        }).ToList();
    }

    public async Task<bool> RetryFailedItemsAsync()
    {
        var failedItems = (await _cloudSyncService.GetPendingSyncItemsAsync())
            .Where(i => i.Status == SyncStatus.Failed)
            .ToList();
        
        foreach (var item in failedItems)
        {
            item.Status = SyncStatus.Pending;
            item.RetryCount = 0;
            item.ErrorMessage = null;
            await _cloudSyncService.QueueSyncItemAsync(item);
        }
        
        await _cloudSyncService.SyncNowAsync();
        return true;
    }

    public async Task<bool> ClearQueueAsync()
    {
        var items = await _cloudSyncService.GetPendingSyncItemsAsync();
        
        foreach (var item in items)
        {
            await _cloudSyncService.CancelSyncItemAsync(item.Id);
        }
        
        return true;
    }
}
