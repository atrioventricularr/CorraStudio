using System.Collections.Concurrent;

namespace CorraStudio.Payments.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly ConcurrentDictionary<string, PaymentStatusResponse> _paymentStatusCache;
    private readonly ILogger<PaymentService>? _logger;

    public event EventHandler<PaymentStatusResponse>? PaymentStatusChanged;

    public PaymentService(IPaymentGatewayFactory gatewayFactory, ILogger<PaymentService>? logger = null)
    {
        _gatewayFactory = gatewayFactory;
        _logger = logger;
        _paymentStatusCache = new ConcurrentDictionary<string, PaymentStatusResponse>();
    }

    public async Task<PaymentResponse> InitiatePaymentAsync(PaymentRequest request)
    {
        // Determine which gateway to use based on payment method
        var gateway = GetGatewayForMethod(request.Method);
        
        if (gateway == null || !gateway.IsConfigured)
        {
            return new PaymentResponse
            {
                Success = false,
                ErrorMessage = $"Payment gateway not available for {request.Method}"
            };
        }
        
        var response = await gateway.CreatePaymentAsync(request);
        
        if (response.Success)
        {
            var statusResponse = new PaymentStatusResponse
            {
                TransactionId = response.TransactionId,
                Status = response.Status,
                ExpiresAt = response.ExpiresAt
            };
            _paymentStatusCache[response.TransactionId] = statusResponse;
            
            // Start background status checking
            _ = Task.Run(() => MonitorPaymentStatus(response.TransactionId, gateway));
        }
        
        return response;
    }

    public async Task<PaymentStatusResponse> GetPaymentStatusAsync(string transactionId)
    {
        if (_paymentStatusCache.TryGetValue(transactionId, out var cached))
        {
            return cached;
        }
        
        // Try all gateways to find the transaction
        foreach (var gateway in GetAllGateways())
        {
            try
            {
                var status = await gateway.CheckStatusAsync(transactionId);
                if (status.Status != PaymentStatus.Pending)
                {
                    _paymentStatusCache[transactionId] = status;
                    return status;
                }
            }
            catch { }
        }
        
        return new PaymentStatusResponse
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Pending
        };
    }

    public async Task<bool> CancelPaymentAsync(string transactionId)
    {
        foreach (var gateway in GetAllGateways())
        {
            try
            {
                var result = await gateway.CancelPaymentAsync(transactionId);
                if (result)
                {
                    _paymentStatusCache.TryRemove(transactionId, out _);
                    return true;
                }
            }
            catch { }
        }
        return false;
    }

    public async Task ProcessWebhookAsync(string gatewayName, string payload, string signature)
    {
        var gateway = _gatewayFactory.CreateGateway(gatewayName);
        var webhookData = await gateway.ParseWebhookAsync(payload, signature);
        
        var statusResponse = new PaymentStatusResponse
        {
            TransactionId = webhookData.TransactionId,
            Status = webhookData.Status,
            PaidAmount = webhookData.Amount,
            PaidAt = webhookData.TransactionTime,
            PaymentMethod = webhookData.PaymentMethod,
            ReferenceNumber = webhookData.ReferenceNumber
        };
        
        _paymentStatusCache[webhookData.TransactionId] = statusResponse;
        PaymentStatusChanged?.Invoke(this, statusResponse);
    }

    public async Task<PaymentStatusResponse> WaitForPaymentAsync(string transactionId, int timeoutSeconds = 300)
    {
        var startTime = DateTime.UtcNow;
        var pollingInterval = TimeSpan.FromSeconds(2);
        
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(timeoutSeconds))
        {
            var status = await GetPaymentStatusAsync(transactionId);
            
            if (status.Status != PaymentStatus.Pending)
            {
                return status;
            }
            
            await Task.Delay(pollingInterval);
        }
        
        return new PaymentStatusResponse
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Expired
        };
    }

    private async Task MonitorPaymentStatus(string transactionId, IPaymentGateway gateway)
    {
        var maxChecks = 30; // 1 minute at 2s intervals
        var checkCount = 0;
        
        while (checkCount < maxChecks)
        {
            await Task.Delay(2000);
            
            var status = await gateway.CheckStatusAsync(transactionId);
            _paymentStatusCache[transactionId] = status;
            
            if (status.Status != PaymentStatus.Pending)
            {
                PaymentStatusChanged?.Invoke(this, status);
                break;
            }
            
            checkCount++;
        }
    }

    private IPaymentGateway? GetGatewayForMethod(PaymentMethod method)
    {
        var gateways = GetAllGateways();
        
        return method switch
        {
            PaymentMethod.QRIS => gateways.FirstOrDefault(g => g.SupportedMethods.Contains(PaymentMethod.QRIS)),
            PaymentMethod.VirtualAccount => gateways.FirstOrDefault(g => g.SupportedMethods.Contains(PaymentMethod.VirtualAccount)),
            PaymentMethod.EWallet => gateways.FirstOrDefault(g => g.SupportedMethods.Contains(PaymentMethod.EWallet)),
            PaymentMethod.ConvenienceStore => gateways.FirstOrDefault(g => g.SupportedMethods.Contains(PaymentMethod.ConvenienceStore)),
            _ => gateways.FirstOrDefault()
        };
    }

    private List<IPaymentGateway> GetAllGateways()
    {
        // This would be replaced with DI in production
        return new List<IPaymentGateway>();
    }
}
