using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace CorraStudio.Infrastructure.Repositories;

public class ConfigurationRepository : IConfigurationRepository
{
    private readonly ILogger<ConfigurationRepository> _logger;
    private static readonly Dictionary<Guid, Configuration> _configurations = new();
    private static readonly object _lock = new();

    public ConfigurationRepository(ILogger<ConfigurationRepository> logger)
    {
        _logger = logger;
    }

    public Task<Configuration?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            _configurations.TryGetValue(id, out var config);
            return Task.FromResult(config);
        }
    }

    public Task<Configuration?> GetByKeyAsync(Guid tenantId, string key)
    {
        lock (_lock)
        {
            var config = _configurations.Values.FirstOrDefault(c => c.TenantId == tenantId && c.Key == key && !c.IsDeleted);
            return Task.FromResult(config);
        }
    }

    public Task<IEnumerable<Configuration>> GetByCategoryAsync(Guid tenantId, string category)
    {
        lock (_lock)
        {
            var configs = _configurations.Values.Where(c => c.TenantId == tenantId && c.Category == category && !c.IsDeleted);
            return Task.FromResult(configs);
        }
    }

    public Task<Configuration> AddAsync(Configuration configuration)
    {
        lock (_lock)
        {
            _configurations[configuration.Id] = configuration;
            _logger.LogInformation("Configuration added: {ConfigId} - {Key}", configuration.Id, configuration.Key);
            return Task.FromResult(configuration);
        }
    }

    public Task UpdateAsync(Configuration configuration)
    {
        lock (_lock)
        {
            if (_configurations.ContainsKey(configuration.Id))
            {
                _configurations[configuration.Id] = configuration;
                _logger.LogInformation("Configuration updated: {ConfigId} - {Key}", configuration.Id, configuration.Key);
            }
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            if (_configurations.ContainsKey(id))
            {
                _configurations.Remove(id);
                _logger.LogInformation("Configuration deleted: {ConfigId}", id);
            }
            return Task.CompletedTask;
        }
    }

    public async Task<T> GetValueAsync<T>(Guid tenantId, string key, T defaultValue)
    {
        var config = await GetByKeyAsync(tenantId, key);
        if (config == null)
            return defaultValue;
        
        try
        {
            return JsonSerializer.Deserialize<T>(config.Value) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetValueAsync<T>(Guid tenantId, string key, T value, string category)
    {
        var existing = await GetByKeyAsync(tenantId, key);
        var serializedValue = JsonSerializer.Serialize(value);
        
        if (existing != null)
        {
            existing.UpdateValue(serializedValue);
            await UpdateAsync(existing);
        }
        else
        {
            var config = new Configuration(tenantId, key, serializedValue, category);
            await AddAsync(config);
        }
    }
}
