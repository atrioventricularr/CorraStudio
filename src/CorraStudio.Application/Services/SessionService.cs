using CorraStudio.Application.DTOs;
using CorraStudio.Application.Queries.Session;

namespace CorraStudio.Application.Services;

public interface ISessionService
{
    Task<SessionDto?> GetCurrentSessionAsync(Guid tenantId);
    Task<string> GenerateSessionCodeAsync(Guid tenantId);
    Task<bool> ValidateSessionAsync(Guid sessionId);
}

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMediator _mediator;

    public SessionService(ISessionRepository sessionRepository, IMediator mediator)
    {
        _sessionRepository = sessionRepository;
        _mediator = mediator;
    }

    public async Task<SessionDto?> GetCurrentSessionAsync(Guid tenantId)
    {
        var sessions = await _sessionRepository.GetActiveSessionsAsync(tenantId);
        var currentSession = sessions.FirstOrDefault();
        return currentSession?.ToDto();
    }

    public async Task<string> GenerateSessionCodeAsync(Guid tenantId)
    {
        var code = $"S{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
        var existing = await _sessionRepository.GetByCodeAsync(code);
        
        while (existing != null)
        {
            code = $"S{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
            existing = await _sessionRepository.GetByCodeAsync(code);
        }
        
        return code;
    }

    public async Task<bool> ValidateSessionAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null) return false;
        
        return session.Status == Domain.Enums.SessionStatus.Active ||
               session.Status == Domain.Enums.SessionStatus.Capturing ||
               session.Status == Domain.Enums.SessionStatus.Reviewing;
    }
}
