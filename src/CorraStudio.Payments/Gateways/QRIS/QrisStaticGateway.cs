using System.Text;
using System.Text.Json;
using SkiaSharp;

namespace CorraStudio.Payments.Gateways.QRIS;

public class QrisStaticGateway : IPaymentGateway
{
    private QrisStaticConfig _config;
    private readonly ILogger<QrisStaticGateway>? _logger;
    private readonly Dictionary<string, QrisTransaction> _transactions;
    private readonly object _lock = new();

    public string Name => "QRIS Static";
    public bool IsConfigured => _config != null && !string.IsNullOrEmpty(_config.QrCodeData) && _config.IsActive;
    public List<PaymentMethod> SupportedMethods => new() { PaymentMethod.QRIS };

    public QrisStaticGateway(QrisStaticConfig config, ILogger<QrisStaticGateway>? logger = null)
    {
        _config = config;
        _logger = logger;
        _transactions = new Dictionary<string, QrisTransaction>();
    }

    public async Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request)
    {
        return await Task.Run(() =>
        {
            if (!IsConfigured)
                return new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = "QRIS Static not configured"
                };
            
            // Check if amount is valid
            if (!IsAmountValid(request.Amount))
            {
                return new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = GetAmountErrorMessage(request.Amount)
                };
            }
            
            // Generate transaction
            var transaction = new QrisTransaction
            {
                Id = Guid.NewGuid(),
                SessionId = Guid.Parse(request.TransactionId.ToString()),
                TransactionCode = GenerateTransactionCode(),
                Amount = request.Amount,
                Status = QrisTransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            
            lock (_lock)
            {
                _transactions[transaction.TransactionCode] = transaction;
            }
            
            // Start monitoring
            _ = Task.Run(() => MonitorTransaction(transaction.TransactionCode));
            
            // Generate QR code (static or dynamic with amount)
            var qrData = GenerateQrCodeData(request.Amount);
            
            return new PaymentResponse
            {
                Success = true,
                TransactionId = transaction.TransactionCode,
                QrCodeData = qrData,
                Status = PaymentStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddMinutes(request.PaymentExpiryMinutes),
                Metadata = new Dictionary<string, object>
                {
                    ["merchantName"] = _config.MerchantName,
                    ["merchantCity"] = _config.MerchantCity,
                    ["amount"] = request.Amount,
                    ["isStatic"] = request.Amount == _config.FixedAmount
                }
            };
        });
    }

    public async Task<PaymentStatusResponse> CheckStatusAsync(string transactionId)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_transactions.TryGetValue(transactionId, out var transaction))
                {
                    return new PaymentStatusResponse
                    {
                        TransactionId = transactionId,
                        Status = MapStatus(transaction.Status),
                        PaidAmount = transaction.Status == QrisTransactionStatus.Paid ? transaction.Amount : null,
                        PaidAt = transaction.PaidAt,
                        ReferenceNumber = transaction.TransactionCode
                    };
                }
            }
            
            return new PaymentStatusResponse
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Pending
            };
        });
    }

    public async Task<bool> CancelPaymentAsync(string transactionId)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_transactions.TryGetValue(transactionId, out var transaction))
                {
                    if (transaction.Status == QrisTransactionStatus.Pending)
                    {
                        transaction.Status = QrisTransactionStatus.Cancelled;
                        _logger?.LogInformation("Transaction {TransactionId} cancelled", transactionId);
                        return true;
                    }
                }
                return false;
            }
        });
    }

    public async Task<PaymentWebhookPayload> ParseWebhookAsync(string rawPayload, string signature)
    {
        return await Task.Run(() =>
        {
            // QRIS Static doesn't have webhook, manual confirmation needed
            return new PaymentWebhookPayload
            {
                TransactionId = string.Empty,
                Status = PaymentStatus.Pending
            };
        });
    }

    public bool VerifyWebhookSignature(string payload, string signature, string? secret = null)
    {
        return true;
    }

    #region Public Methods

    public async Task<bool> ConfirmPaymentAsync(string transactionCode, string paidBy = "Customer")
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_transactions.TryGetValue(transactionCode, out var transaction))
                {
                    if (transaction.Status == QrisTransactionStatus.Pending || 
                        transaction.Status == QrisTransactionStatus.WaitingConfirmation)
                    {
                        transaction.Status = QrisTransactionStatus.Paid;
                        transaction.PaidAt = DateTime.UtcNow;
                        transaction.PaidBy = paidBy;
                        _logger?.LogInformation("Transaction {TransactionCode} confirmed as paid", transactionCode);
                        return true;
                    }
                }
                return false;
            }
        });
    }

    public async Task<bool> MarkAsWaitingConfirmationAsync(string transactionCode)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_transactions.TryGetValue(transactionCode, out var transaction))
                {
                    if (transaction.Status == QrisTransactionStatus.Pending)
                    {
                        transaction.Status = QrisTransactionStatus.WaitingConfirmation;
                        return true;
                    }
                }
                return false;
            }
        });
    }

    public async Task<QrisTransaction?> GetTransactionAsync(string transactionCode)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                return _transactions.TryGetValue(transactionCode, out var transaction) ? transaction : null;
            }
        });
    }

    public async Task<List<QrisTransaction>> GetTodayTransactionsAsync()
    {
        return await Task.Run(() =>
        {
            var today = DateTime.UtcNow.Date;
            lock (_lock)
            {
                return _transactions.Values
                    .Where(t => t.CreatedAt.Date == today)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();
            }
        });
    }

    public async Task<byte[]> GenerateQrCodeImageAsync(decimal amount)
    {
        return await Task.Run(() =>
        {
            var qrData = GenerateQrCodeData(amount);
            return GenerateQrBitmap(qrData);
        });
    }

    #endregion

    #region Private Methods

    private bool IsAmountValid(decimal amount)
    {
        if (amount == _config.FixedAmount)
            return true;
        
        if (_config.AllowCustomAmount)
        {
            if (_config.MinAmount.HasValue && amount < _config.MinAmount.Value)
                return false;
            if (_config.MaxAmount.HasValue && amount > _config.MaxAmount.Value)
                return false;
            return true;
        }
        
        return _config.AvailableAmounts.Contains(amount.ToString());
    }

    private string GetAmountErrorMessage(decimal amount)
    {
        if (amount != _config.FixedAmount && !_config.AllowCustomAmount)
        {
            var available = string.Join(", ", _config.AvailableAmounts);
            return $"Amount must be {_config.FixedAmount:C} or one of: {available}";
        }
        
        if (_config.MinAmount.HasValue && amount < _config.MinAmount.Value)
            return $"Minimum amount is {_config.MinAmount.Value:C}";
        
        if (_config.MaxAmount.HasValue && amount > _config.MaxAmount.Value)
            return $"Maximum amount is {_config.MaxAmount.Value:C}";
        
        return "Invalid amount";
    }

    private string GenerateTransactionCode()
    {
        return $"QRIS-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 30);
    }

    private string GenerateQrCodeData(decimal amount)
    {
        if (amount == _config.FixedAmount)
        {
            // Use static QR code data
            return _config.QrCodeData;
        }
        
        // Generate dynamic QR with amount (if supported by QRIS standard)
        // Format: https://github.com/gerrywang/qris-php
        var qrisData = new StringBuilder();
        qrisData.Append("0002010102112663"); // QRIS header
        qrisData.Append(_config.MerchantId.PadLeft(12, '0'));
        qrisData.Append(_config.MerchantName);
        qrisData.Append(_config.MerchantCity);
        qrisData.Append($"010{amount:F2}");
        qrisData.Append("5802ID");
        qrisData.Append("6207");
        qrisData.Append(DateTime.Now.ToString("yyMMddHHmmss"));
        
        return qrisData.ToString();
    }

    private byte[] GenerateQrBitmap(string qrData)
    {
        try
        {
            using var qrGenerator = new SkiaSharp.QrCode.SKQrCode();
            var qrCode = qrGenerator.Generate(qrData);
            var matrix = qrCode.GetMatrix();
            
            var size = matrix.Size;
            var scale = 10; // pixels per module
            var imageSize = size * scale;
            
            using var surface = SKSurface.Create(new SKImageInfo(imageSize, imageSize));
            var canvas = surface.Canvas;
            
            canvas.Clear(SKColors.White);
            
            using var blackPaint = new SKPaint { Color = SKColors.Black };
            using var whitePaint = new SKPaint { Color = SKColors.White };
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var isBlack = matrix[x, y];
                    var rect = new SKRect(x * scale, y * scale, (x + 1) * scale, (y + 1) * scale);
                    canvas.DrawRect(rect, isBlack ? blackPaint : whitePaint);
                }
            }
            
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch
        {
            // Fallback: return placeholder
            return Array.Empty<byte>();
        }
    }

    private async Task MonitorTransaction(string transactionCode)
    {
        var maxWaitMinutes = 60;
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(maxWaitMinutes))
        {
            lock (_lock)
            {
                if (_transactions.TryGetValue(transactionCode, out var transaction))
                {
                    if (transaction.Status == QrisTransactionStatus.Paid || 
                        transaction.Status == QrisTransactionStatus.Cancelled ||
                        transaction.Status == QrisTransactionStatus.Failed)
                    {
                        break;
                    }
                    
                    if (transaction.Status == QrisTransactionStatus.WaitingConfirmation)
                    {
                        // Auto-expire after 5 minutes in waiting state
                        if ((DateTime.UtcNow - transaction.CreatedAt).TotalMinutes > 5)
                        {
                            transaction.Status = QrisTransactionStatus.Expired;
                            break;
                        }
                    }
                }
            }
            
            await Task.Delay(5000);
        }
        
        lock (_lock)
        {
            if (_transactions.TryGetValue(transactionCode, out var transaction))
            {
                if (transaction.Status == QrisTransactionStatus.Pending ||
                    transaction.Status == QrisTransactionStatus.WaitingConfirmation)
                {
                    transaction.Status = QrisTransactionStatus.Expired;
                    _logger?.LogInformation("Transaction {TransactionCode} expired", transactionCode);
                }
            }
        }
    }

    private PaymentStatus MapStatus(QrisTransactionStatus status)
    {
        return status switch
        {
            QrisTransactionStatus.Pending => PaymentStatus.Pending,
            QrisTransactionStatus.WaitingConfirmation => PaymentStatus.Processing,
            QrisTransactionStatus.Paid => PaymentStatus.Success,
            QrisTransactionStatus.Failed => PaymentStatus.Failed,
            QrisTransactionStatus.Expired => PaymentStatus.Expired,
            QrisTransactionStatus.Cancelled => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
    }

    #endregion
}
