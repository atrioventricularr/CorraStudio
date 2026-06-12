using MediatR;
using CorraStudio.Application.DTOs;
using CorraStudio.Domain.ValueObjects;

namespace CorraStudio.Application.Commands.Photo;

public class CapturePhotoCommand : IRequest<ApiResponse<PhotoDto>>
{
    public Guid SessionId { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public int OrderIndex { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
}

public class CapturePhotoCommandHandler : IRequestHandler<CapturePhotoCommand, ApiResponse<PhotoDto>>
{
    private readonly IPhotoRepository _photoRepository;
    private readonly ISessionRepository _sessionRepository;

    public CapturePhotoCommandHandler(IPhotoRepository photoRepository, ISessionRepository sessionRepository)
    {
        _photoRepository = photoRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<PhotoDto>> Handle(CapturePhotoCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId);
        if (session == null)
            return ApiResponse<PhotoDto>.Fail("Session not found");

        var dimensions = new Dimensions(request.Width, request.Height);
        var photo = new Domain.Entities.Photo(
            session.TenantId ?? Guid.Empty,
            request.SessionId,
            request.FilePath,
            request.ThumbnailPath,
            dimensions,
            request.FileSizeBytes,
            request.OrderIndex
        );

        var result = await _photoRepository.AddAsync(photo);
        session.AddPhoto();
        await _sessionRepository.UpdateAsync(session);

        return ApiResponse<PhotoDto>.Ok(result.ToDto(), "Photo captured successfully");
    }
}
