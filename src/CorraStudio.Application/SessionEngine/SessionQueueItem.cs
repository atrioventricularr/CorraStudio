namespace CorraStudio.Application.SessionEngine;

public class SessionQueueItem
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public SessionQueueType Type { get; set; }
    public SessionQueuePriority Priority { get; set; }
    public SessionQueueStatus Status { get; set; }
    public object? Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
}

public enum SessionQueueType
{
    NewSession = 0,
    CapturePhoto = 1,
    ProcessPhoto = 2,
    PrintPhoto = 3,
    ProcessPayment = 4,
    GenerateQR = 5,
    SyncToCloud = 6,
    SendEmail = 7,
    GenerateGif = 8
}

public enum SessionQueuePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

public enum SessionQueueStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
