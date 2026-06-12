using MediatR;
using CorraStudio.Payments.Models;
using CorraStudio.Payments.Services;

namespace CorraStudio.Application.Payment.Commands;

public class CreatePaymentCommand : IRequest<ApiResponse<PaymentResponse>>
{
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public CustomerInfo Customer { get; set; } = new();
    public string? ReturnUrl { get; set; }
}

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, ApiResponse<PaymentResponse>>
{
    private readonly IPaymentService _paymentService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IPaymentRepository _paymentRepository;

    public CreatePaymentCommandHandler(
        IPaymentService paymentService,
        ISessionRepository sessionRepository,
        IPaymentRepository paymentRepository)
    {
        _paymentService = paymentService;
        _sessionRepository = sessionRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<ApiResponse<PaymentResponse>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId);
        if (session == null)
            return ApiResponse<PaymentResponse>.Fail("Session not found");
        
        var transactionCode = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 30);
        
        var paymentRequest = new PaymentRequest
        {
            TransactionId = Guid.NewGuid(),
            InvoiceNumber = transactionCode,
            Amount = request.Amount,
            Method = request.Method,
            Customer = request.Customer,
            ReturnUrl = request.ReturnUrl,
            PaymentExpiryMinutes = 60
        };
        
        var response = await _paymentService.InitiatePaymentAsync(paymentRequest);
        
        if (response.Success)
        {
            // Save payment record
            var money = new Money(request.Amount);
            var payment = new Domain.Entities.PaymentTransaction(
                request.TenantId,
                request.SessionId,
                transactionCode,
                money,
                Enum.Parse<Domain.Enums.PaymentMethod>(request.Method.ToString())
            );
            
            if (!string.IsNullOrEmpty(response.QrCodeData))
                payment.SetQrCode(response.QrCodeData);
            
            await _paymentRepository.AddAsync(payment);
            
            session.SetPayment(money);
            await _sessionRepository.UpdateAsync(session);
        }
        
        return response.Success 
            ? ApiResponse<PaymentResponse>.Ok(response, "Payment initiated")
            : ApiResponse<PaymentResponse>.Fail(response.ErrorMessage ?? "Payment failed");
    }
}
