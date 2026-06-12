using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Enums;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ILogger<PaymentRepository> _logger;
    private static readonly Dictionary<Guid, PaymentTransaction> _payments = new();
    private static readonly object _lock = new();

    public PaymentRepository(ILogger<PaymentRepository> logger)
    {
        _logger = logger;
    }

    public Task<PaymentTransaction?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            _payments.TryGetValue(id, out var payment);
            return Task.FromResult(payment);
        }
    }

    public Task<PaymentTransaction?> GetByTransactionCodeAsync(string transactionCode)
    {
        lock (_lock)
        {
            var payment = _payments.Values.FirstOrDefault(p => p.TransactionCode == transactionCode);
            return Task.FromResult(payment);
        }
    }

    public Task<IEnumerable<PaymentTransaction>> GetBySessionAsync(Guid sessionId)
    {
        lock (_lock)
        {
            var payments = _payments.Values.Where(p => p.SessionId == sessionId && !p.IsDeleted);
            return Task.FromResult(payments);
        }
    }

    public Task<PaymentTransaction> AddAsync(PaymentTransaction transaction)
    {
        lock (_lock)
        {
            _payments[transaction.Id] = transaction;
            _logger.LogInformation("Payment added: {PaymentId} - Session: {SessionId}", transaction.Id, transaction.SessionId);
            return Task.FromResult(transaction);
        }
    }

    public Task UpdateAsync(PaymentTransaction transaction)
    {
        lock (_lock)
        {
            if (_payments.ContainsKey(transaction.Id))
            {
                _payments[transaction.Id] = transaction;
                _logger.LogInformation("Payment updated: {PaymentId} - Status: {Status}", transaction.Id, transaction.Status);
            }
            return Task.CompletedTask;
        }
    }

    public Task<IEnumerable<PaymentTransaction>> GetPendingPaymentsAsync()
    {
        lock (_lock)
        {
            var pendingStatuses = new[] { PaymentStatus.Pending, PaymentStatus.Processing };
            var payments = _payments.Values.Where(p => pendingStatuses.Contains(p.Status) && !p.IsDeleted);
            return Task.FromResult(payments);
        }
    }
}
