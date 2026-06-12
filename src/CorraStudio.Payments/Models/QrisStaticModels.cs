namespace CorraStudio.Payments.Models;

public class QrisStaticConfig
{
    public Guid Id { get; set; }
    public string QrCodeData { get; set; } = string.Empty;
    public decimal FixedAmount { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCity { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> AvailableAmounts { get; set; } = new();
    public bool AllowCustomAmount { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}

public class QrisPaymentRequest
{
    public Guid SessionId { get; set; }
    public decimal Amount { get; set; }
    public bool UseStaticQr { get; set; } = true;
    public string? CustomAmountNote { get; set; }
}

public class QrisPaymentResponse
{
    public bool Success { get; set; }
    public string QrCodeData { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsStatic { get; set; } = true;
}

public class QrisTransaction
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public QrisTransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidBy { get; set; }
    public string? Notes { get; set; }
}

public enum QrisTransactionStatus
{
    Pending = 0,
    WaitingConfirmation = 1,
    Paid = 2,
    Failed = 3,
    Expired = 4,
    Cancelled = 5
}
