using System.Collections.ObjectModel;

namespace CorraStudio.Application.SessionEngine;

public interface ISessionEngine
{
    event EventHandler<SessionEventArgs>? SessionCreated;
    event EventHandler<SessionEventArgs>? SessionStarted;
    event EventHandler<SessionEventArgs>? SessionPaused;
    event EventHandler<SessionEventArgs>? SessionResumed;
    event EventHandler<SessionEventArgs>? SessionCompleted;
    event EventHandler<SessionEventArgs>? SessionCancelled;
    event EventHandler<SessionEventArgs>? SessionExpired;
    event EventHandler<QueueItemEventArgs>? QueueItemAdded;
    event EventHandler<QueueItemEventArgs>? QueueItemProcessed;
    event EventHandler<QueueItemEventArgs>? QueueItemFailed;
    
    Task<Domain.Entities.Session> CreateSessionAsync(Guid tenantId, SessionConfig? config = null);
    Task<bool> StartSessionAsync(Guid sessionId);
    Task<bool> PauseSessionAsync(Guid sessionId);
    Task<bool> ResumeSessionAsync(Guid sessionId);
    Task<bool> CompleteSessionAsync(Guid sessionId);
    Task<bool> CancelSessionAsync(Guid sessionId, string reason);
    Task<bool> ExpireSessionAsync(Guid sessionId);
    
    Task<SessionQueueItem> EnqueueAsync(SessionQueueItem item);
    Task<bool> CancelQueueItemAsync(Guid itemId);
    Task<SessionQueueItem?> GetQueueItemAsync(Guid itemId);
    Task<List<SessionQueueItem>> GetPendingQueueItemsAsync();
    Task<int> GetQueueLengthAsync();
    
    Domain.Entities.Session? GetSession(Guid sessionId);
    List<Domain.Entities.Session> GetActiveSessions();
    List<Domain.Entities.Session> GetSessionsByTenant(Guid tenantId);
    Task<int> GetActiveSessionCountAsync(Guid tenantId);
    Task<bool> IsSessionActiveAsync(Guid sessionId);
    Task<SessionStatistics> GetStatisticsAsync(Guid tenantId);
    
    Task<int> ProcessQueueAsync();
    Task StartBackgroundProcessingAsync();
    Task StopBackgroundProcessingAsync();
}

public class SessionConfig
{
    public int MaxPhotos { get; set; } = 4;
    public int SessionTimeoutMinutes { get; set; } = 30;
    public Guid? LayoutId { get; set; }
    public Guid? TemplateId { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
}

public class SessionEventArgs : EventArgs
{
    public Guid SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Data { get; set; }
}

public class QueueItemEventArgs : EventArgs
{
    public Guid ItemId { get; set; }
    public Guid SessionId { get; set; }
    public SessionQueueType Type { get; set; }
    public SessionQueueStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SessionStatistics
{
    public int TotalSessionsToday { get; set; }
    public int ActiveSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int CancelledSessions { get; set; }
    public int AverageSessionDurationMinutes { get; set; }
    public int TotalPhotosCaptured { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingQueueItems { get; set; }
    public int FailedQueueItems { get; set; }
}
