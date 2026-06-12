using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Commands.Payment;

public class ProcessPaymentCommand : IRequest<ApiResponse<PaymentDto>>
{
    public Guid PaymentId { get; set; }
    public string ProviderTransactionId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
}

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, ApiResponse<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISessionRepository _sessionRepository;

    public ProcessPaymentCommandHandler(IPaymentRepository paymentRepository, ISessionRepository sessionRepository)
    {
        _paymentRepository = paymentRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<ApiResponse<PaymentDto>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
        if (payment == null)
            return ApiResponse<PaymentDto>.Fail("Payment not found");

        if (request.IsSuccess)
        {
            payment.MarkAsPaid(request.ProviderTransactionId);
            var session = await _sessionRepository.GetByIdAsync(payment.SessionId);
            if (session != null)
            {
                session.CompletePayment();
                await _sessionRepository.UpdateAsync(session);
            }
        }
        else
        {
            payment.MarkAsFailed(request.FailureReason ?? "Payment failed");
        }

        await _paymentRepository.UpdateAsync(payment);
        return ApiResponse<PaymentDto>.Ok(payment.ToDto(), "Payment processed successfully");
    }
}
