using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Commands.Layout;

public class DeleteLayoutCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
}

public class DeleteLayoutCommandHandler : IRequestHandler<DeleteLayoutCommand, ApiResponse<bool>>
{
    private readonly ILayoutRepository _layoutRepository;

    public DeleteLayoutCommandHandler(ILayoutRepository layoutRepository)
    {
        _layoutRepository = layoutRepository;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await _layoutRepository.GetByIdAsync(request.Id);
        if (layout == null)
            return ApiResponse<bool>.Fail("Layout not found");

        await _layoutRepository.DeleteAsync(request.Id);
        return ApiResponse<bool>.Ok(true, "Layout deleted successfully");
    }
}
