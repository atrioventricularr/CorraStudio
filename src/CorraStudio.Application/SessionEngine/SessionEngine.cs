using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Application.SessionEngine;

public class SessionEngine : ISessionEngine
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPrintJobRepository _printJobRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ConcurrentDictionary<Guid, Domain.Entities.Session> _activeSessions;
    private readonly ConcurrentQueue<SessionQueueItem> _queue;
    private readonly ConcurrentDictionary<Guid, SessionQueueItem> _queueItems;
    private readonly Timer _expiryTimer;
    private readonly Timer _queueProcessorTimer;
    private bool _isProcessing;
    private bool _isBackgroundProcessing;
    private readonly object _lock = new();

    public event EventHandler<SessionEventArgs>? SessionCreated;
    public event EventHandler<SessionEventArgs>? SessionStarted;
    public event EventHandler<SessionEventArgs>? SessionPaused;
    public event EventHandler<SessionEventArgs>? SessionResumed;
    public event EventHandler<SessionEventArgs>? SessionCompleted;
    public event EventHandler<SessionEventArgs>? SessionCancelled;
    public event EventHandler<SessionEventArgs>? SessionExpired;
    public event EventHandler<QueueItemEventArgs>? QueueItemAdded;
    public event EventHandler<QueueItemEventArgs>? QueueItemProcessed;
    public event EventHandler<QueueItemEventArgs>? QueueItemFailed;

    public SessionEngine(
        ISessionRepository sessionRepository,
        IPhotoRepository photoRepository,
        IPrintJobRepository printJobRepository,
        IPaymentRepository paymentRepository)
    {
        _sessionRepository = sessionRepository;
        _photoRepository = photoRepository;
        _printJobRepository = printJobRepository;
        _paymentRepository = paymentRepository;
        
        _activeSessions = new ConcurrentDictionary<Guid, Domain.Entities.Session>();
        _queue = new ConcurrentQueue<SessionQueueItem>();
        _queueItems = new ConcurrentDictionary<Guid, SessionQueueItem>();
        
        // Check for expired sessions every minute
        _expiryTimer = new Timer(CheckExpiredSessions, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        
        // Process queue every 5 seconds
        _queueProcessorTimer = new Timer(async _ => await ProcessQueueAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public async Task<Domain.Entities.Session> CreateSessionAsync(Guid tenantId, SessionConfig? config = null)
    {
        var sessionCode = GenerateSessionCode();
        var session = new Domain.Entities.Session(tenantId, sessionCode);
        
        if (config != null)
        {
            if (config.LayoutId.HasValue)
                session.SetLayout(config.LayoutId.Value);
            if (config.TemplateId.HasValue)
                session.SetTemplate(config.TemplateId.Value);
        }
        
        var createdSession = await _sessionRepository.AddAsync(session);
        
        SessionCreated?.Invoke(this, new SessionEventArgs
        {
            SessionId = createdSession.Id,
            SessionCode = createdSession.SessionCode,
            Timestamp = DateTime.UtcNow
        });
        
        return createdSession;
    }

    public async Task<bool> StartSessionAsync(Guid sessionId)
    {
        var session = await GetOrLoadSessionAsync(sessionId);
        if (session == null) return false;
        
        if (session.Status != Domain.Enums.SessionStatus.Pending)
            return false;
        
        session.Start();
        await _sessionRepository.UpdateAsync(session);
        
        _activeSessions.TryAdd(sessionId, session);
        
        SessionStarted?.Invoke(this, new SessionEventArgs
        {
            SessionId = session.Id,
            SessionCode = session.SessionCode,
            Timestamp = DateTime.UtcNow
        });
        
        return true;
    }

    public async Task<bool> PauseSessionAsync(Guid sessionId)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return false;
        
        // Can only pause active sessions
        if (session.Status != Domain.Enums.SessionStatus.Active && 
            session.Status != Domain.Enums.SessionStatus.Capturing)
            return false;
        
        // Store pause state (would need custom implementation)
        
        SessionPaused?.Invoke(this, new SessionEventArgs
        {
            SessionId = session.Id,
            SessionCode = session.SessionCode,
            Timestamp = DateTime.UtcNow
        });
        
        return true;
    }

    public async Task<bool> ResumeSessionAsync(Guid sessionId)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return false;
        
        SessionResumed?.Invoke(this, new SessionEventArgs
        {
            SessionId = session.Id,
            SessionCode = session.SessionCode,
            Timestamp = DateTime.UtcNow
        });
        
        return true;
    }

    public async Task<bool> CompleteSessionAsync(Guid sessionId)
    {
        var session = await GetOrLoadSessionAsync(sessionId);
        if (session == null) return false;
        
        session.Complete();
        await _sessionRepository.UpdateAsync(session);
        
        _activeSessions.TryRemove(sessionId, out _);
        
        SessionCompleted?.Invoke(this, new SessionEventArgs
        {
            SessionId = session.Id,
            SessionCode = session.SessionCode,
            Timestamp = DateTime.UtcNow
        });
        
        return true;
    }

    public async Task<bool> CancelSessionAsync(Guid sessionId, string reason)
    {
        var session = await GetOrLoadSessionAsync(sessionId);
        if (session == null) return false;
        
        session.Cancel(reason);
        await _sessionRepository.UpdateAsync(session);
        
        _activeSessions.TryRemove(sessionId, out _);
        
        // Cancel all pending queue items for this session
        var pendingItems = _queueItems.Values.Where(q => q.SessionId == sessionId && q.Status == SessionQueueStatus.Pending);
        foreach (var item in pendingItems)
        {
            item.Status = SessionQueueStatus.Cancelled;
        }
        
        SessionCancelled?.Invoke(this, new SessionEventArgs
        {
            SessionId = session.Id,
            SessionCode = session.SessionCode,
            Timestamp = DateTime.UtcNow,
            Data = reason
        });
        
        return true;
    }

    public async Task<bool> ExpireSessionAsync(Guid sessionId)
    {
        var session = await GetOrLoadSessionAsync(sessionId);
        if (session == null) return false;
        
        _activeSessions.TryRemove(sessionId, out _);
        
        SessionExpired?.Invoke(this, new SessionEventArgs
        {
            SessionId = session.Id,
            SessionCode = session.SessionCode,
            Timestamp = DateTime.UtcNow
        });
        
        return true;
    }

    public async Task<SessionQueueItem> EnqueueAsync(SessionQueueItem item)
    {
        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTime.UtcNow;
        item.Status = SessionQueueStatus.Pending;
        
        _queue.Enqueue(item);
        _queueItems.TryAdd(item.Id, item);
        
        QueueItemAdded?.Invoke(this, new QueueItemEventArgs
        {
            ItemId = item.Id,
            SessionId = item.SessionId,
            Type = item.Type,
            Status = item.Status,
            Timestamp = DateTime.UtcNow
        });
        
        return await Task.FromResult(item);
    }

    public async Task<bool> CancelQueueItemAsync(Guid itemId)
    {
        if (_queueItems.TryGetValue(itemId, out var item))
        {
            if (item.Status == SessionQueueStatus.Pending)
            {
                item.Status = SessionQueueStatus.Cancelled;
                return true;
            }
        }
        return false;
    }

    public async Task<SessionQueueItem?> GetQueueItemAsync(Guid itemId)
    {
        _queueItems.TryGetValue(itemId, out var item);
        return await Task.FromResult(item);
    }

    public async Task<List<SessionQueueItem>> GetPendingQueueItemsAsync()
    {
        var pending = _queueItems.Values.Where(q => q.Status == SessionQueueStatus.Pending).ToList();
        return await Task.FromResult(pending);
    }

    public async Task<int> GetQueueLengthAsync()
    {
        return await Task.FromResult(_queueItems.Values.Count(q => q.Status == SessionQueueStatus.Pending));
    }

    public Domain.Entities.Session? GetSession(Guid sessionId)
    {
        _activeSessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public List<Domain.Entities.Session> GetActiveSessions()
    {
        return _activeSessions.Values.ToList();
    }

    public List<Domain.Entities.Session> GetSessionsByTenant(Guid tenantId)
    {
        return _activeSessions.Values.Where(s => s.TenantId == tenantId).ToList();
    }

    public async Task<int> GetActiveSessionCountAsync(Guid tenantId)
    {
        return await Task.FromResult(_activeSessions.Values.Count(s => s.TenantId == tenantId));
    }

    public async Task<bool> IsSessionActiveAsync(Guid sessionId)
    {
        return await Task.FromResult(_activeSessions.ContainsKey(sessionId));
    }

    public async Task<SessionStatistics> GetStatisticsAsync(Guid tenantId)
    {
        var today = DateTime.UtcNow.Date;
        var allSessions = await _sessionRepository.GetByTenantAsync(tenantId);
        
        var stats = new SessionStatistics
        {
            TotalSessionsToday = allSessions.Count(s => s.CreatedAt.Date == today),
            ActiveSessions = _activeSessions.Values.Count(s => s.TenantId == tenantId),
            CompletedSessions = allSessions.Count(s => s.Status == Domain.Enums.SessionStatus.Completed),
            CancelledSessions = allSessions.Count(s => s.Status == Domain.Enums.SessionStatus.Cancelled),
            PendingQueueItems = _queueItems.Values.Count(q => q.Status == SessionQueueStatus.Pending),
            FailedQueueItems = _queueItems.Values.Count(q => q.Status == SessionQueueStatus.Failed)
        };
        
        return stats;
    }

    public async Task<int> ProcessQueueAsync()
    {
        if (_isProcessing) return 0;
        
        lock (_lock)
        {
            if (_isProcessing) return 0;
            _isProcessing = true;
        }
        
        var processedCount = 0;
        
        try
        {
            while (_queue.TryDequeue(out var item))
            {
                if (item.Status != SessionQueueStatus.Pending)
                    continue;
                
                item.Status = SessionQueueStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                
                try
                {
                    await ProcessQueueItemAsync(item);
                    item.Status = SessionQueueStatus.Completed;
                    item.CompletedAt = DateTime.UtcNow;
                    
                    QueueItemProcessed?.Invoke(this, new QueueItemEventArgs
                    {
                        ItemId = item.Id,
                        SessionId = item.SessionId,
                        Type = item.Type,
                        Status = item.Status,
                        Timestamp = DateTime.UtcNow
                    });
                    
                    processedCount++;
                }
                catch (Exception ex)
                {
                    item.RetryCount++;
                    
                    if (item.RetryCount >= item.MaxRetries)
                    {
                        item.Status = SessionQueueStatus.Failed;
                        item.ErrorMessage = ex.Message;
                        
                        QueueItemFailed?.Invoke(this, new QueueItemEventArgs
                        {
                            ItemId = item.Id,
                            SessionId = item.SessionId,
                            Type = item.Type,
                            Status = item.Status,
                            Timestamp = DateTime.UtcNow,
                            ErrorMessage = ex.Message
                        });
                    }
                    else
                    {
                        // Requeue for retry
                        item.Status = SessionQueueStatus.Pending;
                        _queue.Enqueue(item);
                    }
                }
            }
        }
        finally
        {
            lock (_lock)
            {
                _isProcessing = false;
            }
        }
        
        return processedCount;
    }

    private async Task ProcessQueueItemAsync(SessionQueueItem item)
    {
        switch (item.Type)
        {
            case SessionQueueType.NewSession:
                // Handle new session creation
                break;
            case SessionQueueType.CapturePhoto:
                // Handle photo capture
                break;
            case SessionQueueType.ProcessPhoto:
                // Handle photo processing
                break;
            case SessionQueueType.PrintPhoto:
                // Handle printing
                break;
            case SessionQueueType.ProcessPayment:
                // Handle payment processing
                break;
            case SessionQueueType.GenerateQR:
                // Handle QR generation
                break;
            case SessionQueueType.SyncToCloud:
                // Handle cloud sync
                break;
            case SessionQueueType.SendEmail:
                // Handle email sending
                break;
            case SessionQueueType.GenerateGif:
                // Handle GIF generation
                break;
        }
        
        await Task.CompletedTask;
    }

    private async void CheckExpiredSessions(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredSessions = _activeSessions.Values
            .Where(s => s.Status == Domain.Enums.SessionStatus.Active && 
                       (now - s.StartedAt)?.TotalMinutes > 30) // 30 minute timeout
            .ToList();
        
        foreach (var session in expiredSessions)
        {
            await ExpireSessionAsync(session.Id);
        }
    }

    public async Task StartBackgroundProcessingAsync()
    {
        if (_isBackgroundProcessing) return;
        _isBackgroundProcessing = true;
        
        while (_isBackgroundProcessing)
        {
            await ProcessQueueAsync();
            await Task.Delay(5000);
        }
    }

    public async Task StopBackgroundProcessingAsync()
    {
        _isBackgroundProcessing = false;
        await Task.CompletedTask;
    }

    private async Task<Domain.Entities.Session?> GetOrLoadSessionAsync(Guid sessionId)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
            return session;
        
        session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session != null)
            _activeSessions.TryAdd(sessionId, session);
        
        return session;
    }

    private string GenerateSessionCode()
    {
        return $"SESSION_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }

    // Helper methods for Session entity (need to add these to Session entity)
    // These are extension methods for now
}

// Extension methods for Session entity
public static class SessionExtensions
{
    public static void SetLayout(this Domain.Entities.Session session, Guid layoutId)
    {
        var property = typeof(Domain.Entities.Session).GetProperty("LayoutId");
        if (property != null && property.CanWrite)
        {
            property.SetValue(session, layoutId);
        }
    }
    
    public static void SetTemplate(this Domain.Entities.Session session, Guid templateId)
    {
        var property = typeof(Domain.Entities.Session).GetProperty("TemplateId");
        if (property != null && property.CanWrite)
        {
            property.SetValue(session, templateId);
        }
    }
}
