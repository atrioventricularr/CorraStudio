using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Queries.Session;

public class GetActiveSessionsQuery : IRequest<ApiResponse<List<SessionDto>>>
{
    public Guid TenantId { get; set; }
}

public class GetActiveSessionsQueryHandler : IRequestHandler<GetActiveSessionsQuery, ApiResponse<List<SessionDto>>>
{
    private readonly ISessionRepository _sessionRepository;

    public GetActiveSessionsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<List<SessionDto>>> Handle(GetActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetActiveSessionsAsync(request.TenantId);
        var dtos = sessions.Select(s => s.ToDto()).ToList();
        return ApiResponse<List<SessionDto>>.Ok(dtos);
    }
}
