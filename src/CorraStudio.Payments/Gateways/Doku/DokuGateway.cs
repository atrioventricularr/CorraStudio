using System.Text;
using System.Text.Json;

namespace CorraStudio.Payments.Gateways.Doku;

public class DokuGateway : IPaymentGateway
{
    private readonly DokuConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<DokuGateway>? _logger;

    public string Name => "Doku";
    public bool IsConfigured => !string.IsNullOrEmpty(_config.ClientId) && !string.IsNullOrEmpty(_config.SharedKey);
    public List<PaymentMethod> SupportedMethods => GetSupportedMethods();

    public DokuGateway(DokuConfiguration config, ILogger<DokuGateway>? logger = null)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Client-Id", _config.ClientId);
    }

    public async Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request)
    {
        try
        {
            var dokuRequest = CreateDokuRequest(request);
            var jsonRequest = JsonSerializer.Serialize(dokuRequest);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            // Generate signature
            var signature = GenerateSignature(request.InvoiceNumber, request.Amount);
            _httpClient.DefaultRequestHeaders.Add("Signature", signature);
            
            var response = await _httpClient.PostAsync($"{_config.ApiUrl}/checkout/v1/payment", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var dokuResponse = JsonSerializer.Deserialize<DokuApiResponse>(responseContent);
                return MapToPaymentResponse(dokuResponse, request);
            }
            
            return new PaymentResponse
            {
                Success = false,
                ErrorMessage = $"Doku API error: {responseContent}"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Doku payment creation failed");
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
            var signature = GenerateSignature(transactionId, 0);
            _httpClient.DefaultRequestHeaders.Add("Signature", signature);
            
            var response = await _httpClient.GetAsync($"{_config.ApiUrl}/checkout/v1/payment/status?order={transactionId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var statusResponse = JsonSerializer.Deserialize<DokuStatusResponse>(responseContent);
                return MapToStatusResponse(statusResponse);
            }
            
            return new PaymentStatusResponse
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Failed,
                ErrorMessage = responseContent
            };
        }
        catch (Exception ex)
        {
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
            var signature = GenerateSignature(transactionId, 0);
            _httpClient.DefaultRequestHeaders.Add("Signature", signature);
            
            var response = await _httpClient.PostAsync($"{_config.ApiUrl}/checkout/v1/payment/cancel", 
                new StringContent($"{{\"order\":\"{transactionId}\"}}", Encoding.UTF8, "application/json"));
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
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
            
            var payload = JsonSerializer.Deserialize<DokuWebhookPayload>(rawPayload);
            
            return new PaymentWebhookPayload
            {
                TransactionId = payload?.Order?.InvoiceNumber ?? string.Empty,
                OrderId = payload?.Order?.OrderNumber ?? string.Empty,
                Status = MapStatus(payload?.Transaction?.Status ?? "PENDING"),
                Amount = payload?.Order?.Amount ?? 0,
                TransactionTime = DateTime.Parse(payload?.Transaction?.TransactionTime ?? DateTime.UtcNow.ToString()),
                PaymentMethod = payload?.Transaction?.PaymentMethod,
                ReferenceNumber = payload?.Transaction?.ReferenceNumber,
                RawData = new Dictionary<string, object>
                {
                    ["raw"] = rawPayload
                }
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse webhook: {ex.Message}");
        }
    }

    public bool VerifyWebhookSignature(string payload, string signature, string? secret = null)
    {
        // Doku signature verification
        var expectedSignature = GenerateSignatureFromPayload(payload);
        return signature == expectedSignature;
    }

    #region Private Methods

    private DokuPaymentRequest CreateDokuRequest(PaymentRequest request)
    {
        var methodTypes = new List<string>();
        
        if (_config.EnableQRIS)
            methodTypes.Add("QRIS");
        if (_config.EnableVirtualAccount)
            methodTypes.Add("VIRTUAL_ACCOUNT");
        if (_config.EnableEWallet)
            methodTypes.Add("EMONEY");
        if (_config.EnableConvenienceStore)
            methodTypes.Add("CONVENIENCE_STORE");
        
        return new DokuPaymentRequest
        {
            Order = new DokuOrder
            {
                Amount = request.Amount,
                InvoiceNumber = request.InvoiceNumber,
                OrderNumber = request.TransactionId.ToString()
            },
            Payment = new DokuPayment
            {
                PaymentDueDate = request.PaymentExpiryMinutes,
                PaymentMethodTypes = methodTypes
            },
            Customer = new DokuCustomer
            {
                Name = request.Customer.Name,
                Email = request.Customer.Email,
                Phone = request.Customer.Phone
            },
            Url = new DokuUrl
            {
                CallbackUrl = request.ReturnUrl ?? "",
                NotifyUrl = request.WebhookUrl ?? ""
            }
        };
    }

    private string GenerateSignature(string invoiceNumber, decimal amount)
    {
        var rawSignature = $"{_config.ClientId}|{invoiceNumber}|{amount}|{_config.SharedKey}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawSignature);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private string GenerateSignatureFromPayload(string payload)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(payload + _config.SharedKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private PaymentResponse MapToPaymentResponse(DokuApiResponse? response, PaymentRequest request)
    {
        if (response == null)
            return new PaymentResponse { Success = false, ErrorMessage = "Empty response" };
        
        return new PaymentResponse
        {
            Success = true,
            TransactionId = request.TransactionId.ToString(),
            PaymentUrl = response.Web?.CheckoutUrl ?? string.Empty,
            QrCodeData = response.Payment?.QrCode?.QrCodeData,
            VirtualAccountNumber = response.Payment?.VirtualAccount?.VaNumber,
            Status = PaymentStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(request.PaymentExpiryMinutes),
            Metadata = new Dictionary<string, object>
            {
                ["orderNumber"] = response.Order?.OrderNumber ?? string.Empty,
                ["checkoutUrl"] = response.Web?.CheckoutUrl ?? string.Empty
            }
        };
    }

    private PaymentStatusResponse MapToStatusResponse(DokuStatusResponse? response)
    {
        if (response == null)
            return new PaymentStatusResponse { Status = PaymentStatus.Failed };
        
        return new PaymentStatusResponse
        {
            TransactionId = response.Order?.InvoiceNumber ?? string.Empty,
            Status = MapStatus(response.Transaction?.Status ?? "PENDING"),
            PaidAmount = response.Transaction?.Amount,
            PaidAt = response.Transaction?.SettlementTime != null ? DateTime.Parse(response.Transaction.SettlementTime) : null,
            PaymentMethod = response.Transaction?.PaymentMethod,
            ReferenceNumber = response.Transaction?.ReferenceNumber
        };
    }

    private PaymentStatus MapStatus(string dokuStatus)
    {
        return dokuStatus.ToUpper() switch
        {
            "SUCCESS" => PaymentStatus.Success,
            "PENDING" => PaymentStatus.Pending,
            "FAILED" => PaymentStatus.Failed,
            "EXPIRED" => PaymentStatus.Expired,
            "CANCELLED" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
    }

    private List<PaymentMethod> GetSupportedMethods()
    {
        var methods = new List<PaymentMethod>();
        if (_config.EnableQRIS) methods.Add(PaymentMethod.QRIS);
        if (_config.EnableVirtualAccount) methods.Add(PaymentMethod.VirtualAccount);
        if (_config.EnableEWallet) methods.Add(PaymentMethod.EWallet);
        if (_config.EnableConvenienceStore) methods.Add(PaymentMethod.ConvenienceStore);
        return methods;
    }

    #endregion
}

#region Doku API Models

internal class DokuPaymentRequest
{
    public DokuOrder Order { get; set; } = new();
    public DokuPayment Payment { get; set; } = new();
    public DokuCustomer Customer { get; set; } = new();
    public DokuUrl Url { get; set; } = new();
}

internal class DokuOrder
{
    public decimal Amount { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
}

internal class DokuPayment
{
    public int PaymentDueDate { get; set; } = 60;
    public List<string> PaymentMethodTypes { get; set; } = new();
}

internal class DokuCustomer
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

internal class DokuUrl
{
    public string CallbackUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
}

internal class DokuApiResponse
{
    public DokuResponseOrder? Order { get; set; }
    public DokuResponsePayment? Payment { get; set; }
    public DokuResponseWeb? Web { get; set; }
}

internal class DokuResponseOrder
{
    public string OrderNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
}

internal class DokuResponsePayment
{
    public DokuResponseQrCode? QrCode { get; set; }
    public DokuResponseVirtualAccount? VirtualAccount { get; set; }
}

internal class DokuResponseQrCode
{
    public string QrCodeData { get; set; } = string.Empty;
}

internal class DokuResponseVirtualAccount
{
    public string VaNumber { get; set; } = string.Empty;
}

internal class DokuResponseWeb
{
    public string CheckoutUrl { get; set; } = string.Empty;
}

internal class DokuStatusResponse
{
    public DokuStatusOrder? Order { get; set; }
    public DokuStatusTransaction? Transaction { get; set; }
}

internal class DokuStatusOrder
{
    public string InvoiceNumber { get; set; } = string.Empty;
}

internal class DokuStatusTransaction
{
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? SettlementTime { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
}

internal class DokuWebhookPayload
{
    public DokuWebhookOrder? Order { get; set; }
    public DokuWebhookTransaction? Transaction { get; set; }
}

internal class DokuWebhookOrder
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

internal class DokuWebhookTransaction
{
    public string Status { get; set; } = string.Empty;
    public string TransactionTime { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
}

#endregion
