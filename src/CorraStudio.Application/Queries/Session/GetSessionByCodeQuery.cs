using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Queries.Session;

public class GetSessionByCodeQuery : IRequest<ApiResponse<SessionDto>>
{
    public string SessionCode { get; set; } = string.Empty;
}

public class GetSessionByCodeQueryHandler : IRequestHandler<GetSessionByCodeQuery, ApiResponse<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository;

    public GetSessionByCodeQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<SessionDto>> Handle(GetSessionByCodeQuery request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByCodeAsync(request.SessionCode);
        if (session == null)
            return ApiResponse<SessionDto>.Fail("Session not found");

        return ApiResponse<SessionDto>.Ok(session.ToDto());
    }
}
