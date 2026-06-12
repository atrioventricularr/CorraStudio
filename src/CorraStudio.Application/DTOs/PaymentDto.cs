namespace CorraStudio.Application.DTOs;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public string? QrCodeData { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? FailureReason { get; set; }
}

public class CreatePaymentDto
{
    public Guid SessionId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
}

public class ProcessPaymentDto
{
    public Guid PaymentId { get; set; }
    public string ProviderTransactionId { get; set; } = string.Empty;
}
