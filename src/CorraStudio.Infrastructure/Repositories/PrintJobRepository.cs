using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Enums;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Infrastructure.Repositories;

public class PrintJobRepository : IPrintJobRepository
{
    private readonly ILogger<PrintJobRepository> _logger;
    private static readonly Dictionary<Guid, PrintJob> _printJobs = new();
    private static readonly object _lock = new();

    public PrintJobRepository(ILogger<PrintJobRepository> logger)
    {
        _logger = logger;
    }

    public Task<PrintJob?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            _printJobs.TryGetValue(id, out var printJob);
            return Task.FromResult(printJob);
        }
    }

    public Task<IEnumerable<PrintJob>> GetBySessionAsync(Guid sessionId)
    {
        lock (_lock)
        {
            var jobs = _printJobs.Values.Where(j => j.SessionId == sessionId && !j.IsDeleted);
            return Task.FromResult(jobs);
        }
    }

    public Task<IEnumerable<PrintJob>> GetPendingJobsAsync()
    {
        lock (_lock)
        {
            var pendingStatuses = new[] { PrintStatus.Queued };
            var jobs = _printJobs.Values.Where(j => pendingStatuses.Contains(j.Status) && !j.IsDeleted);
            return Task.FromResult(jobs);
        }
    }

    public Task<PrintJob> AddAsync(PrintJob printJob)
    {
        lock (_lock)
        {
            _printJobs[printJob.Id] = printJob;
            _logger.LogInformation("PrintJob added: {PrintJobId} - Session: {SessionId}", printJob.Id, printJob.SessionId);
            return Task.FromResult(printJob);
        }
    }

    public Task UpdateAsync(PrintJob printJob)
    {
        lock (_lock)
        {
            if (_printJobs.ContainsKey(printJob.Id))
            {
                _printJobs[printJob.Id] = printJob;
                _logger.LogInformation("PrintJob updated: {PrintJobId} - Status: {Status}", printJob.Id, printJob.Status);
            }
            return Task.CompletedTask;
        }
    }

    public Task<IEnumerable<PrintJob>> GetFailedJobsAsync()
    {
        lock (_lock)
        {
            var jobs = _printJobs.Values.Where(j => j.Status == PrintStatus.Failed && j.CanRetry() && !j.IsDeleted);
            return Task.FromResult(jobs);
        }
    }
}
