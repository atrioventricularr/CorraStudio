using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Enums;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Infrastructure.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly ILogger<SessionRepository> _logger;
    private static readonly Dictionary<Guid, Session> _sessions = new();
    private static readonly object _lock = new();

    public SessionRepository(ILogger<SessionRepository> logger)
    {
        _logger = logger;
    }

    public Task<Session?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            _sessions.TryGetValue(id, out var session);
            return Task.FromResult(session);
        }
    }

    public Task<Session?> GetByCodeAsync(string sessionCode)
    {
        lock (_lock)
        {
            var session = _sessions.Values.FirstOrDefault(s => s.SessionCode == sessionCode);
            return Task.FromResult(session);
        }
    }

    public Task<IEnumerable<Session>> GetByTenantAsync(Guid tenantId)
    {
        lock (_lock)
        {
            var sessions = _sessions.Values.Where(s => s.TenantId == tenantId && !s.IsDeleted);
            return Task.FromResult(sessions);
        }
    }

    public Task<IEnumerable<Session>> GetByStatusAsync(Guid tenantId, SessionStatus status)
    {
        lock (_lock)
        {
            var sessions = _sessions.Values.Where(s => s.TenantId == tenantId && s.Status == status && !s.IsDeleted);
            return Task.FromResult(sessions);
        }
    }

    public Task<IEnumerable<Session>> GetActiveSessionsAsync(Guid tenantId)
    {
        lock (_lock)
        {
            var activeStatuses = new[] { SessionStatus.Active, SessionStatus.Capturing, SessionStatus.Reviewing, SessionStatus.PaymentPending, SessionStatus.Processing };
            var sessions = _sessions.Values.Where(s => s.TenantId == tenantId && activeStatuses.Contains(s.Status) && !s.IsDeleted);
            return Task.FromResult(sessions);
        }
    }

    public Task<Session> AddAsync(Session session)
    {
        lock (_lock)
        {
            _sessions[session.Id] = session;
            _logger.LogInformation("Session added: {SessionId} - {SessionCode}", session.Id, session.SessionCode);
            return Task.FromResult(session);
        }
    }

    public Task UpdateAsync(Session session)
    {
        lock (_lock)
        {
            if (_sessions.ContainsKey(session.Id))
            {
                _sessions[session.Id] = session;
                _logger.LogInformation("Session updated: {SessionId} - {SessionCode}", session.Id, session.SessionCode);
            }
            return Task.CompletedTask;
        }
    }

    public Task<int> GetSessionCountTodayAsync(Guid tenantId)
    {
        lock (_lock)
        {
            var today = DateTime.UtcNow.Date;
            var count = _sessions.Values.Count(s => s.TenantId == tenantId && s.CreatedAt.Date == today && !s.IsDeleted);
            return Task.FromResult(count);
        }
    }
}
