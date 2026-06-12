using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Interfaces.Services;
using CorraStudio.Domain.ValueObjects;

namespace CorraStudio.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    private readonly ILogger<QrCodeService> _logger;
    private static readonly Dictionary<string, (Guid PhotoId, DateTime Expiry)> _tokens = new();
    private static readonly object _lock = new();

    public QrCodeService(ILogger<QrCodeService> logger)
    {
        _logger = logger;
    }

    public Task<QrCode> GenerateForDownloadAsync(Guid photoId, string downloadToken)
    {
        var data = $"corra://download?token={downloadToken}&photo={photoId}";
        var qrCode = new QrCode(data);
        
        lock (_lock)
        {
            _tokens[downloadToken] = (photoId, DateTime.UtcNow.AddHours(24));
        }
        
        _logger.LogInformation("QR Code generated for download: {PhotoId}", photoId);
        return Task.FromResult(qrCode);
    }

    public Task<QrCode> GenerateForPaymentAsync(PaymentTransaction transaction)
    {
        var data = $"corra://payment?id={transaction.Id}&code={transaction.TransactionCode}&amount={transaction.Amount.Amount}";
        var qrCode = new QrCode(data);
        
        _logger.LogInformation("QR Code generated for payment: {TransactionId}", transaction.Id);
        return Task.FromResult(qrCode);
    }

    public Task<QrCode> GenerateForSessionAsync(Session session)
    {
        var data = $"corra://session?code={session.SessionCode}";
        var qrCode = new QrCode(data);
        
        _logger.LogInformation("QR Code generated for session: {SessionId}", session.Id);
        return Task.FromResult(qrCode);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        lock (_lock)
        {
            if (_tokens.TryGetValue(token, out var tokenInfo))
            {
                if (tokenInfo.Expiry > DateTime.UtcNow)
                {
                    _tokens.Remove(token);
                    return Task.FromResult(true);
                }
                _tokens.Remove(token);
            }
            return Task.FromResult(false);
        }
    }
}
