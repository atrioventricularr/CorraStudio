using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Commands.Session;

public class CompleteSessionCommand : IRequest<ApiResponse<SessionDto>>
{
    public Guid SessionId { get; set; }
}

public class CompleteSessionCommandHandler : IRequestHandler<CompleteSessionCommand, ApiResponse<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository;

    public CompleteSessionCommandHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<SessionDto>> Handle(CompleteSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId);
        if (session == null)
            return ApiResponse<SessionDto>.Fail("Session not found");

        session.Complete();
        await _sessionRepository.UpdateAsync(session);

        return ApiResponse<SessionDto>.Ok(session.ToDto(), "Session completed successfully");
    }
}
