using MediatR;
using CorraStudio.Application.DTOs;
using CorraStudio.Domain.ValueObjects;

namespace CorraStudio.Application.Commands.Layout;

public class CreateLayoutCommand : IRequest<ApiResponse<LayoutDto>>
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string ConfigJson { get; set; } = string.Empty;
}

public class CreateLayoutCommandHandler : IRequestHandler<CreateLayoutCommand, ApiResponse<LayoutDto>>
{
    private readonly ILayoutRepository _layoutRepository;
    private readonly ITenantRepository _tenantRepository;

    public CreateLayoutCommandHandler(ILayoutRepository layoutRepository, ITenantRepository tenantRepository)
    {
        _layoutRepository = layoutRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<ApiResponse<LayoutDto>> Handle(CreateLayoutCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId);
        if (tenant == null)
            return ApiResponse<LayoutDto>.Fail("Tenant not found");

        var exists = await _layoutRepository.ExistsByNameAsync(request.TenantId, request.Name);
        if (exists)
            return ApiResponse<LayoutDto>.Fail("Layout name already exists");

        var dimensions = new Dimensions(request.Width, request.Height);
        var layout = new Domain.Entities.Layout(
            request.TenantId,
            request.Name,
            request.Description,
            dimensions,
            request.ConfigJson
        );

        var result = await _layoutRepository.AddAsync(layout);
        return ApiResponse<LayoutDto>.Ok(result.ToDto(), "Layout created successfully");
    }
}
