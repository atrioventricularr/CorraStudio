namespace CorraStudio.Payments.Models;

public class MidtransConfiguration
{
    public string ClientKey { get; set; } = string.Empty;
    public string ServerKey { get; set; } = string.Empty;
    public bool IsProduction { get; set; } = false;
    public bool EnableQRIS { get; set; } = true;
    public bool EnableCreditCard { get; set; } = true;
    public bool EnableVirtualAccount { get; set; } = true;
    public bool EnableEwallet { get; set; } = true;
    public bool EnableConvenienceStore { get; set; } = false;
    public string? PaymentNotificationUrl { get; set; }
    public string? FinishRedirectUrl { get; set; }
    public string? UnfinishRedirectUrl { get; set; }
    public string? ErrorRedirectUrl { get; set; }
    
    public string ApiUrl => IsProduction 
        ? "https://api.midtrans.com/v2"
        : "https://api.sandbox.midtrans.com/v2";
    
    public string SnapUrl => IsProduction
        ? "https://app.midtrans.com/snap/v1"
        : "https://app.sandbox.midtrans.com/snap/v1";
}

public class MidtransPaymentRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IDR";
    public CustomerInfo Customer { get; set; } = new();
    public List<MidtransItemDetail> Items { get; set; } = new();
    public MidtransPaymentMethod PaymentMethod { get; set; } = MidtransPaymentMethod.QRIS;
    public string? Bank { get; set; }
    public int ExpiryMinutes { get; set; } = 60;
}

public class MidtransItemDetail
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Quantity { get; set; }
}

public enum MidtransPaymentMethod
{
    QRIS = 0,
    CreditCard = 1,
    VirtualAccount = 2,
    Ewallet = 3,
    ConvenienceStore = 4
}

public class MidtransPaymentResponse
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
    public string? QrCodeData { get; set; }
    public string? VirtualAccountNumber { get; set; }
    public string? PaymentCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class MidtransStatusResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string TransactionStatus { get; set; } = string.Empty;
    public string FraudStatus { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public DateTime? SettlementTime { get; set; }
    public DateTime? TransactionTime { get; set; }
    public string? PaymentCode { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? VaNumber { get; set; }
    public string? Bank { get; set; }
    public string? PermataVaNumber { get; set; }
}

public class MidtransNotification
{
    public string OrderId { get; set; } = string.Empty;
    public string TransactionStatus { get; set; } = string.Empty;
    public string FraudStatus { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusMessage { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? TransactionTime { get; set; }
    public DateTime? SettlementTime { get; set; }
    public string? Currency { get; set; }
    public string? SignatureKey { get; set; }
}
