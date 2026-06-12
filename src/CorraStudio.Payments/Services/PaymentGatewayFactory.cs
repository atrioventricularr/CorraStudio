using Microsoft.Extensions.DependencyInjection;
using CorraStudio.Payments.Gateways.Doku;
using CorraStudio.Payments.Gateways.Midtrans;
using CorraStudio.Payments.Gateways.QRIS;

namespace CorraStudio.Payments.Services;

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<string, IPaymentGateway> _gateways;

    public PaymentGatewayFactory(
        IServiceProvider serviceProvider,
        IConfigurationRepository configurationRepository,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _configurationRepository = configurationRepository;
        _loggerFactory = loggerFactory;
        _gateways = new Dictionary<string, IPaymentGateway>();
    }

    public IPaymentGateway CreateGateway(string gatewayName)
    {
        if (_gateways.TryGetValue(gatewayName, out var gateway))
            return gateway;
        
        var newGateway = gatewayName.ToLower() switch
        {
            "doku" => CreateDokuGateway(),
            "midtrans" => CreateMidtransGateway(),
            "qris" => CreateQrisGateway(),
            _ => throw new Exception($"Unknown gateway: {gatewayName}")
        };
        
        _gateways[gatewayName] = newGateway;
        return newGateway;
    }

    public List<string> GetAvailableGateways()
    {
        return new List<string> { "doku", "midtrans", "qris" };
    }

    private IPaymentGateway CreateDokuGateway()
    {
        var config = new DokuConfiguration
        {
            ClientId = GetSetting("Payment.Doku.ClientId") ?? "",
            SharedKey = GetSetting("Payment.Doku.SharedKey") ?? "",
            Environment = GetSetting("Payment.Doku.Environment") ?? "sandbox",
            EnableQRIS = GetSetting("Payment.Doku.EnableQRIS") == "true",
            EnableVirtualAccount = GetSetting("Payment.Doku.EnableVirtualAccount") == "true",
            EnableEWallet = GetSetting("Payment.Doku.EnableEWallet") == "true",
            EnableConvenienceStore = GetSetting("Payment.Doku.EnableConvenienceStore") == "true"
        };
        
        var logger = _loggerFactory.CreateLogger<DokuGateway>();
        return new DokuGateway(config, logger);
    }

    private IPaymentGateway CreateMidtransGateway()
    {
        var config = new MidtransConfiguration
        {
            ClientKey = GetSetting("Payment.Midtrans.ClientKey") ?? "",
            ServerKey = GetSetting("Payment.Midtrans.ServerKey") ?? "",
            IsProduction = GetSetting("Payment.Midtrans.IsProduction") == "true",
            EnableQRIS = GetSetting("Payment.Midtrans.EnableQRIS") != "false",
            EnableCreditCard = GetSetting("Payment.Midtrans.EnableCreditCard") != "false",
            EnableVirtualAccount = GetSetting("Payment.Midtrans.EnableVirtualAccount") != "false",
            EnableEwallet = GetSetting("Payment.Midtrans.EnableEwallet") != "false",
            EnableConvenienceStore = GetSetting("Payment.Midtrans.EnableConvenienceStore") == "true",
            PaymentNotificationUrl = GetSetting("Payment.Midtrans.NotificationUrl"),
            FinishRedirectUrl = GetSetting("Payment.Midtrans.FinishUrl"),
            UnfinishRedirectUrl = GetSetting("Payment.Midtrans.UnfinishUrl"),
            ErrorRedirectUrl = GetSetting("Payment.Midtrans.ErrorUrl")
        };
        
        var logger = _loggerFactory.CreateLogger<MidtransGateway>();
        return new MidtransGateway(config, logger);
    }

    private IPaymentGateway CreateQrisGateway()
    {
        var config = new QrisStaticConfig
        {
            QrCodeData = GetSetting("Payment.QRIS.StaticData") ?? "",
            FixedAmount = decimal.Parse(GetSetting("Payment.QRIS.FixedAmount") ?? "50000"),
            MerchantName = GetSetting("Payment.QRIS.MerchantName") ?? "Corra Studio",
            MerchantCity = GetSetting("Payment.QRIS.MerchantCity") ?? "Jakarta",
            MerchantId = GetSetting("Payment.QRIS.MerchantId") ?? "123456789012",
            IsActive = GetSetting("Payment.QRIS.IsActive") != "false",
            AllowCustomAmount = GetSetting("Payment.QRIS.AllowCustomAmount") == "true",
            MinAmount = decimal.TryParse(GetSetting("Payment.QRIS.MinAmount"), out var min) ? min : null,
            MaxAmount = decimal.TryParse(GetSetting("Payment.QRIS.MaxAmount"), out var max) ? max : null
        };
        
        var amounts = GetSetting("Payment.QRIS.AvailableAmounts");
        if (!string.IsNullOrEmpty(amounts))
        {
            config.AvailableAmounts = amounts.Split(',').ToList();
        }
        
        var logger = _loggerFactory.CreateLogger<QrisStaticGateway>();
        return new QrisStaticGateway(config, logger);
    }

    private string? GetSetting(string key)
    {
        // In production, would query database
        // For demo, return null (gateway will show as not configured)
        return null;
    }
}
