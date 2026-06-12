using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Commands.Session;

public class CreateSessionCommand : IRequest<ApiResponse<SessionDto>>
{
    public Guid TenantId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public Guid? LayoutId { get; set; }
    public Guid? TemplateId { get; set; }
}

public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, ApiResponse<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ITenantRepository _tenantRepository;

    public CreateSessionCommandHandler(ISessionRepository sessionRepository, ITenantRepository tenantRepository)
    {
        _sessionRepository = sessionRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<ApiResponse<SessionDto>> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId);
        if (tenant == null)
            return ApiResponse<SessionDto>.Fail("Tenant not found");

        var existingSession = await _sessionRepository.GetByCodeAsync(request.SessionCode);
        if (existingSession != null)
            return ApiResponse<SessionDto>.Fail("Session code already exists");

        var session = new Domain.Entities.Session(request.TenantId, request.SessionCode);
        
        if (request.LayoutId.HasValue)
        {
            var layout = await _layoutRepository.GetByIdAsync(request.LayoutId.Value);
            if (layout != null)
                session.SetLayout(request.LayoutId.Value);
        }

        var result = await _sessionRepository.AddAsync(session);
        return ApiResponse<SessionDto>.Ok(result.ToDto(), "Session created successfully");
    }
}
