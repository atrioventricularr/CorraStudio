using MediatR;
using CorraStudio.Application.DTOs;
using CorraStudio.Domain.ValueObjects;

namespace CorraStudio.Application.Commands.Layout;

public class UpdateLayoutCommand : IRequest<ApiResponse<LayoutDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string ConfigJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateLayoutCommandHandler : IRequestHandler<UpdateLayoutCommand, ApiResponse<LayoutDto>>
{
    private readonly ILayoutRepository _layoutRepository;

    public UpdateLayoutCommandHandler(ILayoutRepository layoutRepository)
    {
        _layoutRepository = layoutRepository;
    }

    public async Task<ApiResponse<LayoutDto>> Handle(UpdateLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await _layoutRepository.GetByIdAsync(request.Id);
        if (layout == null)
            return ApiResponse<LayoutDto>.Fail("Layout not found");

        var dimensions = new Dimensions(request.Width, request.Height);
        layout.Update(request.Name, request.Description, dimensions, request.ConfigJson);
        layout.SetActive(request.IsActive);
        layout.SetDisplayOrder(request.DisplayOrder);

        await _layoutRepository.UpdateAsync(layout);
        return ApiResponse<LayoutDto>.Ok(layout.ToDto(), "Layout updated successfully");
    }
}
