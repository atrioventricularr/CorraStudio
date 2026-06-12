namespace CorraStudio.Sync.CloudSync;

public class SyncQueueItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public SyncEntityType EntityType { get; set; }
    public SyncOperation Operation { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public SyncStatus Status { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 5;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int Priority { get; set; } = 1;
}

public enum SyncEntityType
{
    Session = 0,
    Photo = 1,
    PrintJob = 2,
    Payment = 3,
    Configuration = 4,
    Layout = 5,
    Template = 6,
    User = 7
}

public enum SyncOperation
{
    Create = 0,
    Update = 1,
    Delete = 2,
    SoftDelete = 3
}

public enum SyncStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public class SyncStatistics
{
    public int PendingItems { get; set; }
    public int ProcessingItems { get; set; }
    public int CompletedToday { get; set; }
    public int FailedItems { get; set; }
    public DateTime LastSyncTime { get; set; }
    public TimeSpan AverageSyncTime { get; set; }
    public bool IsCloudConnected { get; set; }
    public Dictionary<SyncEntityType, int> ItemsByEntity { get; set; } = new();
}

public class SyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemsSynced { get; set; }
    public TimeSpan Duration { get; set; }
}

public class CloudSyncConfig
{
    public bool EnableAutoSync { get; set; } = true;
    public int SyncIntervalSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 5;
    public int BatchSize { get; set; } = 50;
    public bool SyncOnStartup { get; set; } = true;
    public bool SyncOnSessionComplete { get; set; } = true;
    public string? CloudApiUrl { get; set; }
    public string? CloudApiKey { get; set; }
}
