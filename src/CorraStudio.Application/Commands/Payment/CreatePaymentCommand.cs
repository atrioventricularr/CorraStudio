using MediatR;
using CorraStudio.Application.DTOs;
using CorraStudio.Domain.Enums;
using CorraStudio.Domain.ValueObjects;

namespace CorraStudio.Application.Commands.Payment;

public class CreatePaymentCommand : IRequest<ApiResponse<PaymentDto>>
{
    public Guid SessionId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
}

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, ApiResponse<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISessionRepository _sessionRepository;

    public CreatePaymentCommandHandler(IPaymentRepository paymentRepository, ISessionRepository sessionRepository)
    {
        _paymentRepository = paymentRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<PaymentDto>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId);
        if (session == null)
            return ApiResponse<PaymentDto>.Fail("Session not found");

        var method = Enum.Parse<PaymentMethod>(request.Method);
        var money = new Money(request.Amount);
        var transactionCode = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 30);

        var payment = new Domain.Entities.PaymentTransaction(
            session.TenantId ?? Guid.Empty,
            request.SessionId,
            transactionCode,
            money,
            method
        );

        var result = await _paymentRepository.AddAsync(payment);
        session.SetPayment(money);
        await _sessionRepository.UpdateAsync(session);

        return ApiResponse<PaymentDto>.Ok(result.ToDto(), "Payment created successfully");
    }
}
