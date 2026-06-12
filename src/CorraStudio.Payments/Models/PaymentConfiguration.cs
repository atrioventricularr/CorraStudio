namespace CorraStudio.Payments.Models;

public class DokuConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string SharedKey { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox"; // sandbox / production
    public bool EnableQRIS { get; set; } = true;
    public bool EnableVirtualAccount { get; set; } = true;
    public bool EnableEWallet { get; set; } = true;
    public bool EnableConvenienceStore { get; set; } = false;
    public string ApiUrl => Environment == "production" 
        ? "https://api.doku.com" 
        : "https://api-sandbox.doku.com";
    public string CheckoutUrl => Environment == "production"
        ? "https://checkout.doku.com"
        : "https://checkout-sandbox.doku.com";
}

public class MidtransConfiguration
{
    public string ClientKey { get; set; } = string.Empty;
    public string ServerKey { get; set; } = string.Empty;
    public bool IsProduction { get; set; } = false;
    public bool EnableQRIS { get; set; } = true;
    public bool EnableCreditCard { get; set; } = true;
}

public class XenditConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public bool IsProduction { get; set; } = false;
    public bool EnableQRIS { get; set; } = true;
    public bool EnableVirtualAccount { get; set; } = true;
}

public class QrisStaticConfiguration
{
    public string QrCodeData { get; set; } = string.Empty;
    public decimal FixedAmount { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCity { get; set; } = string.Empty;
}
