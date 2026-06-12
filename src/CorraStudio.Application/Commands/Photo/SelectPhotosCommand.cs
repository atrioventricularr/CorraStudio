using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Commands.Photo;

public class SelectPhotosCommand : IRequest<ApiResponse<bool>>
{
    public Guid SessionId { get; set; }
    public List<Guid> PhotoIds { get; set; } = new();
}

public class SelectPhotosCommandHandler : IRequestHandler<SelectPhotosCommand, ApiResponse<bool>>
{
    private readonly IPhotoRepository _photoRepository;

    public SelectPhotosCommandHandler(IPhotoRepository photoRepository)
    {
        _photoRepository = photoRepository;
    }

    public async Task<ApiResponse<bool>> Handle(SelectPhotosCommand request, CancellationToken cancellationToken)
    {
        var photos = await _photoRepository.GetBySessionAsync(request.SessionId);
        
        foreach (var photo in photos)
        {
            if (request.PhotoIds.Contains(photo.Id))
                photo.Select();
            else
                photo.Unselect();
            
            await _photoRepository.UpdateAsync(photo);
        }

        return ApiResponse<bool>.Ok(true, "Photos selected successfully");
    }
}
