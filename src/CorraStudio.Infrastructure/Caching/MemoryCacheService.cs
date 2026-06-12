using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CorraStudio.Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task ClearAsync();
}

public class MemoryCacheService : ICacheService
{
    private readonly ILogger<MemoryCacheService> _logger;
    private static readonly ConcurrentDictionary<string, (object Value, DateTime Expiry)> _cache = new();
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

    public MemoryCacheService(ILogger<MemoryCacheService> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached.Expiry > DateTime.UtcNow)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult((T?)cached.Value);
            }
            
            _cache.TryRemove(key, out _);
            _logger.LogDebug("Cache expired for key: {Key}", key);
        }
        
        _logger.LogDebug("Cache miss for key: {Key}", key);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var expiry = DateTime.UtcNow.Add(expiration ?? DefaultExpiration);
        _cache[key] = (value!, expiry);
        _logger.LogDebug("Cache set for key: {Key}, expires at: {Expiry}", key, expiry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.TryRemove(key, out _);
        _logger.LogDebug("Cache removed for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached.Expiry > DateTime.UtcNow)
                return Task.FromResult(true);
            
            _cache.TryRemove(key, out _);
        }
        return Task.FromResult(false);
    }

    public Task ClearAsync()
    {
        _cache.Clear();
        _logger.LogInformation("Cache cleared");
        return Task.CompletedTask;
    }
}
