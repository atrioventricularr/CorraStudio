namespace CorraStudio.Payments.Abstractions;

public interface IPaymentGateway
{
    string Name { get; }
    bool IsConfigured { get; }
    List<PaymentMethod> SupportedMethods { get; }
    
    Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request);
    Task<PaymentStatusResponse> CheckStatusAsync(string transactionId);
    Task<bool> CancelPaymentAsync(string transactionId);
    Task<PaymentWebhookPayload> ParseWebhookAsync(string rawPayload, string signature);
    bool VerifyWebhookSignature(string payload, string signature, string? secret = null);
}

public interface IPaymentGatewayFactory
{
    IPaymentGateway CreateGateway(string gatewayName);
    List<string> GetAvailableGateways();
}

public interface IPaymentService
{
    event EventHandler<PaymentStatusResponse>? PaymentStatusChanged;
    
    Task<PaymentResponse> InitiatePaymentAsync(PaymentRequest request);
    Task<PaymentStatusResponse> GetPaymentStatusAsync(string transactionId);
    Task<bool> CancelPaymentAsync(string transactionId);
    Task ProcessWebhookAsync(string gatewayName, string payload, string signature);
    Task<PaymentStatusResponse> WaitForPaymentAsync(string transactionId, int timeoutSeconds = 300);
}
