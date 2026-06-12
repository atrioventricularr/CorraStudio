namespace CorraStudio.Payments.Models;

public class PaymentRequest
{
    public Guid TransactionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IDR";
    public PaymentMethod Method { get; set; }
    public CustomerInfo Customer { get; set; } = new();
    public Dictionary<string, string> CustomFields { get; set; } = new();
    public int PaymentExpiryMinutes { get; set; } = 60;
    public string? ReturnUrl { get; set; }
    public string? WebhookUrl { get; set; }
}

public class PaymentResponse
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string? QrCodeData { get; set; }
    public string? VirtualAccountNumber { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PaymentStatusResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CustomerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
}

public class PaymentWebhookPayload
{
    public string TransactionId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionTime { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public Dictionary<string, object> RawData { get; set; } = new();
}

public enum PaymentMethod
{
    QRIS = 0,
    VirtualAccount = 1,
    CreditCard = 2,
    EWallet = 3,
    ConvenienceStore = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Success = 2,
    Failed = 3,
    Expired = 4,
    Cancelled = 5
}
