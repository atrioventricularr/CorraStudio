using MediatR;
using CorraStudio.Payments.Models;
using CorraStudio.Payments.Services;

namespace CorraStudio.Application.Payment.Queries;

public class GetPaymentStatusQuery : IRequest<ApiResponse<PaymentStatusResponse>>
{
    public string TransactionId { get; set; } = string.Empty;
}

public class GetPaymentStatusQueryHandler : IRequestHandler<GetPaymentStatusQuery, ApiResponse<PaymentStatusResponse>>
{
    private readonly IPaymentService _paymentService;

    public GetPaymentStatusQueryHandler(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task<ApiResponse<PaymentStatusResponse>> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var status = await _paymentService.GetPaymentStatusAsync(request.TransactionId);
        return ApiResponse<PaymentStatusResponse>.Ok(status);
    }
}
