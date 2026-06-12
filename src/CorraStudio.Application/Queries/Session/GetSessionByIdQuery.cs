using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Queries.Session;

public class GetSessionByIdQuery : IRequest<ApiResponse<SessionDto>>
{
    public Guid Id { get; set; }
}

public class GetSessionByIdQueryHandler : IRequestHandler<GetSessionByIdQuery, ApiResponse<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository;

    public GetSessionByIdQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<SessionDto>> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.Id);
        if (session == null)
            return ApiResponse<SessionDto>.Fail("Session not found");

        return ApiResponse<SessionDto>.Ok(session.ToDto());
    }
}
