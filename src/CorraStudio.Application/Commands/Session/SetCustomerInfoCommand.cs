using MediatR;
using CorraStudio.Application.DTOs;
using CorraStudio.Domain.ValueObjects;

namespace CorraStudio.Application.Commands.Session;

public class SetCustomerInfoCommand : IRequest<ApiResponse<SessionDto>>
{
    public Guid SessionId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
}

public class SetCustomerInfoCommandHandler : IRequestHandler<SetCustomerInfoCommand, ApiResponse<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository;

    public SetCustomerInfoCommandHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<SessionDto>> Handle(SetCustomerInfoCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId);
        if (session == null)
            return ApiResponse<SessionDto>.Fail("Session not found");

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            email = new Email(request.CustomerEmail);

        PhoneNumber? phone = null;
        if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
            phone = new PhoneNumber(request.CustomerPhone);

        session.SetCustomerInfo(request.CustomerName, email, phone);
        await _sessionRepository.UpdateAsync(session);

        return ApiResponse<SessionDto>.Ok(session.ToDto(), "Customer info updated successfully");
    }
}
