using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Commands.Session;

public class CancelSessionCommand : IRequest<ApiResponse<SessionDto>>
{
    public Guid SessionId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CancelSessionCommandHandler : IRequestHandler<CancelSessionCommand, ApiResponse<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository;

    public CancelSessionCommandHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<SessionDto>> Handle(CancelSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId);
        if (session == null)
            return ApiResponse<SessionDto>.Fail("Session not found");

        session.Cancel(request.Reason);
        await _sessionRepository.UpdateAsync(session);

        return ApiResponse<SessionDto>.Ok(session.ToDto(), "Session cancelled successfully");
    }
}
