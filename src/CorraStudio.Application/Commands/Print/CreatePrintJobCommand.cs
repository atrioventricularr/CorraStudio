using MediatR;
using CorraStudio.Application.DTOs;
using CorraStudio.Domain.Enums;

namespace CorraStudio.Application.Commands.Print;

public class CreatePrintJobCommand : IRequest<ApiResponse<PrintJobDto>>
{
    public Guid SessionId { get; set; }
    public Guid PhotoId { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public string PrinterType { get; set; } = string.Empty;
    public int CopyCount { get; set; } = 1;
}

public class CreatePrintJobCommandHandler : IRequestHandler<CreatePrintJobCommand, ApiResponse<PrintJobDto>>
{
    private readonly IPrintJobRepository _printJobRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly ISessionRepository _sessionRepository;

    public CreatePrintJobCommandHandler(
        IPrintJobRepository printJobRepository,
        IPhotoRepository photoRepository,
        ISessionRepository sessionRepository)
    {
        _printJobRepository = printJobRepository;
        _photoRepository = photoRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<PrintJobDto>> Handle(CreatePrintJobCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId);
        if (session == null)
            return ApiResponse<PrintJobDto>.Fail("Session not found");

        var photo = await _photoRepository.GetByIdAsync(request.PhotoId);
        if (photo == null)
            return ApiResponse<PrintJobDto>.Fail("Photo not found");

        var printerType = Enum.Parse<PrinterType>(request.PrinterType);
        
        var printJob = new Domain.Entities.PrintJob(
            session.TenantId ?? Guid.Empty,
            request.SessionId,
            request.PhotoId,
            request.PrinterName,
            printerType,
            request.CopyCount
        );

        var result = await _printJobRepository.AddAsync(printJob);
        return ApiResponse<PrintJobDto>.Ok(result.ToDto(), "Print job created successfully");
    }
}
