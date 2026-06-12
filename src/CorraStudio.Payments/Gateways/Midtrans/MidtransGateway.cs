using System.Text;
using System.Text.Json;

namespace CorraStudio.Payments.Gateways.Midtrans;

public class MidtransGateway : IPaymentGateway
{
    private readonly MidtransConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MidtransGateway>? _logger;
    private readonly Dictionary<string, MidtransStatusResponse> _statusCache;

    public string Name => "Midtrans";
    public bool IsConfigured => !string.IsNullOrEmpty(_config.ClientKey) && !string.IsNullOrEmpty(_config.ServerKey);
    public List<PaymentMethod> SupportedMethods => GetSupportedMethods();

    public MidtransGateway(MidtransConfiguration config, ILogger<MidtransGateway>? logger = null)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient();
        _statusCache = new Dictionary<string, MidtransStatusResponse>();
        
        // Set authentication
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ServerKey}:"));
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {auth}");
    }

    public async Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request)
    {
        try
        {
            var midtransRequest = CreateSnapRequest(request);
            var jsonRequest = JsonSerializer.Serialize(midtransRequest);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_config.SnapUrl}/transactions", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var snapResponse = JsonSerializer.Deserialize<MidtransSnapResponse>(responseContent);
                return MapToPaymentResponse(snapResponse, request);
            }
            
            _logger?.LogError("Midtrans API error: {Response}", responseContent);
            return new PaymentResponse
            {
                Success = false,
                ErrorMessage = $"Midtrans error: {responseContent}"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Midtrans payment creation failed");
            return new PaymentResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentStatusResponse> CheckStatusAsync(string transactionId)
    {
        try
        {
            // Check cache first
            if (_statusCache.TryGetValue(transactionId, out var cached))
            {
                return MapToStatusResponse(cached);
            }
            
            var response = await _httpClient.GetAsync($"{_config.ApiUrl}/{transactionId}/status");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var statusResponse = JsonSerializer.Deserialize<MidtransStatusResponse>(responseContent);
                if (statusResponse != null)
                {
                    _statusCache[transactionId] = statusResponse;
                    return MapToStatusResponse(statusResponse);
                }
            }
            
            return new PaymentStatusResponse
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Pending
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Status check failed for {TransactionId}", transactionId);
            return new PaymentStatusResponse
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> CancelPaymentAsync(string transactionId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_config.ApiUrl}/{transactionId}/cancel", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Cancel failed for {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<PaymentWebhookPayload> ParseWebhookAsync(string rawPayload, string signature)
    {
        try
        {
            // Verify signature
            if (!VerifyWebhookSignature(rawPayload, signature))
                throw new Exception("Invalid webhook signature");
            
            var notification = JsonSerializer.Deserialize<MidtransNotification>(rawPayload);
            
            if (notification == null)
                throw new Exception("Failed to parse webhook payload");
            
            return new PaymentWebhookPayload
            {
                TransactionId = notification.OrderId,
                OrderId = notification.OrderId,
                Status = MapWebhookStatus(notification.TransactionStatus),
                Amount = notification.GrossAmount,
                TransactionTime = notification.TransactionTime ?? DateTime.UtcNow,
                PaymentMethod = notification.PaymentType,
                ReferenceNumber = notification.TransactionId,
                RawData = new Dictionary<string, object>
                {
                    ["fraudStatus"] = notification.FraudStatus,
                    ["statusCode"] = notification.StatusCode ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Webhook parsing failed");
            throw;
        }
    }

    public bool VerifyWebhookSignature(string payload, string signature, string? secret = null)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<MidtransNotification>(payload);
            if (notification == null) return false;
            
            var expectedSignature = GenerateSignatureKey(notification);
            return signature == expectedSignature;
        }
        catch
        {
            return false;
        }
    }

    #region Private Methods

    private MidtransSnapRequest CreateSnapRequest(PaymentRequest request)
    {
        var items = new List<MidtransSnapItem>
        {
            new MidtransSnapItem
            {
                Id = "PHOTO-" + DateTime.Now.ToString("yyyyMMdd"),
                Name = "Photo Session",
                Price = (int)request.Amount,
                Quantity = 1
            }
        };
        
        return new MidtransSnapRequest
        {
            TransactionDetails = new MidtransTransactionDetails
            {
                OrderId = request.InvoiceNumber,
                GrossAmount = (int)request.Amount
            },
            CreditCard = new MidtransCreditCard
            {
                Secure = true
            },
            CustomerDetails = new MidtransCustomerDetails
            {
                FirstName = request.Customer.Name.Split(' ').FirstOrDefault() ?? "Customer",
                LastName = string.Join(" ", request.Customer.Name.Split(' ').Skip(1)),
                Email = request.Customer.Email,
                Phone = request.Customer.Phone
            },
            ItemDetails = items,
            Expiry = new MidtransExpiry
            {
                Duration = request.PaymentExpiryMinutes,
                Unit = "minute"
            },
            Callbacks = new MidtransCallbacks
            {
                Finish = _config.FinishRedirectUrl ?? request.ReturnUrl ?? "",
                Unfinish = _config.UnfinishRedirectUrl ?? "",
                Error = _config.ErrorRedirectUrl ?? ""
            }
        };
    }

    private PaymentResponse MapToPaymentResponse(MidtransSnapResponse? response, PaymentRequest request)
    {
        if (response == null)
            return new PaymentResponse { Success = false, ErrorMessage = "Empty response" };
        
        return new PaymentResponse
        {
            Success = true,
            TransactionId = request.InvoiceNumber,
            Token = response.Token,
            PaymentUrl = response.RedirectUrl,
            Status = PaymentStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(request.PaymentExpiryMinutes),
            Metadata = new Dictionary<string, object>
            {
                ["token"] = response.Token,
                ["redirectUrl"] = response.RedirectUrl
            }
        };
    }

    private PaymentStatusResponse MapToStatusResponse(MidtransStatusResponse response)
    {
        return new PaymentStatusResponse
        {
            TransactionId = response.OrderId,
            Status = MapStatus(response.TransactionStatus),
            PaidAmount = response.GrossAmount,
            PaidAt = response.SettlementTime,
            PaymentMethod = response.PaymentType,
            ReferenceNumber = response.TransactionId,
            ErrorMessage = response.StatusMessage
        };
    }

    private PaymentStatus MapStatus(string midtransStatus)
    {
        return midtransStatus.ToLower() switch
        {
            "capture" => PaymentStatus.Success,
            "settlement" => PaymentStatus.Success,
            "pending" => PaymentStatus.Pending,
            "deny" => PaymentStatus.Failed,
            "cancel" => PaymentStatus.Cancelled,
            "expire" => PaymentStatus.Expired,
            "failure" => PaymentStatus.Failed,
            _ => PaymentStatus.Pending
        };
    }

    private PaymentStatus MapWebhookStatus(string transactionStatus)
    {
        return transactionStatus.ToLower() switch
        {
            "capture" => PaymentStatus.Success,
            "settlement" => PaymentStatus.Success,
            "pending" => PaymentStatus.Processing,
            _ => PaymentStatus.Processing
        };
    }

    private string GenerateSignatureKey(MidtransNotification notification)
    {
        var signatureString = $"{notification.OrderId}{notification.StatusCode}{notification.GrossAmount}{_config.ServerKey}";
        using var sha512 = System.Security.Cryptography.SHA512.Create();
        var bytes = Encoding.UTF8.GetBytes(signatureString);
        var hash = sha512.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private List<PaymentMethod> GetSupportedMethods()
    {
        var methods = new List<PaymentMethod>();
        if (_config.EnableQRIS) methods.Add(PaymentMethod.QRIS);
        if (_config.EnableCreditCard) methods.Add(PaymentMethod.CreditCard);
        if (_config.EnableVirtualAccount) methods.Add(PaymentMethod.VirtualAccount);
        if (_config.EnableEwallet) methods.Add(PaymentMethod.EWallet);
        if (_config.EnableConvenienceStore) methods.Add(PaymentMethod.ConvenienceStore);
        return methods;
    }

    #endregion
}

#region Midtrans API Models

internal class MidtransSnapRequest
{
    public MidtransTransactionDetails TransactionDetails { get; set; } = new();
    public MidtransCreditCard CreditCard { get; set; } = new();
    public MidtransCustomerDetails CustomerDetails { get; set; } = new();
    public List<MidtransSnapItem> ItemDetails { get; set; } = new();
    public MidtransExpiry Expiry { get; set; } = new();
    public MidtransCallbacks Callbacks { get; set; } = new();
}

internal class MidtransTransactionDetails
{
    public string OrderId { get; set; } = string.Empty;
    public int GrossAmount { get; set; }
}

internal class MidtransCreditCard
{
    public bool Secure { get; set; } = true;
}

internal class MidtransCustomerDetails
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

internal class MidtransSnapItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Quantity { get; set; }
}

internal class MidtransExpiry
{
    public int Duration { get; set; }
    public string Unit { get; set; } = "minute";
}

internal class MidtransCallbacks
{
    public string Finish { get; set; } = string.Empty;
    public string Unfinish { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

internal class MidtransSnapResponse
{
    public string Token { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
}

#endregion
