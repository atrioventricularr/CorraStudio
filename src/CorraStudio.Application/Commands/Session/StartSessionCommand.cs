using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Commands.Session;

public class StartSessionCommand : IRequest<ApiResponse<SessionDto>>
{
    public Guid SessionId { get; set; }
}

public class StartSessionCommandHandler : IRequestHandler<StartSessionCommand, ApiResponse<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository;

    public StartSessionCommandHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<SessionDto>> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId);
        if (session == null)
            return ApiResponse<SessionDto>.Fail("Session not found");

        session.Start();
        await _sessionRepository.UpdateAsync(session);

        return ApiResponse<SessionDto>.Ok(session.ToDto(), "Session started successfully");
    }
}
