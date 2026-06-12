using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CorraStudio.Application.SessionEngine;

namespace CorraStudio.Infrastructure.BackgroundServices;

public class SessionWorkerService : BackgroundService
{
    private readonly ISessionEngine _sessionEngine;
    private readonly ILogger<SessionWorkerService> _logger;

    public SessionWorkerService(ISessionEngine sessionEngine, ILogger<SessionWorkerService> logger)
    {
        _sessionEngine = sessionEngine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Worker Service started");
        
        await _sessionEngine.StartBackgroundProcessingAsync();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queueLength = await _sessionEngine.GetQueueLengthAsync();
                if (queueLength > 0)
                {
                    _logger.LogDebug("Processing {QueueLength} items in queue", queueLength);
                }
                
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in session worker");
                await Task.Delay(5000, stoppingToken);
            }
        }
        
        await _sessionEngine.StopBackgroundProcessingAsync();
        _logger.LogInformation("Session Worker Service stopped");
    }
}
