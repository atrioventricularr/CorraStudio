using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly ILogger<TenantRepository> _logger;
    private static readonly Dictionary<Guid, Tenant> _tenants = new();
    private static readonly object _lock = new();

    public TenantRepository(ILogger<TenantRepository> logger)
    {
        _logger = logger;
    }

    public Task<Tenant?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            _tenants.TryGetValue(id, out var tenant);
            return Task.FromResult(tenant);
        }
    }

    public Task<Tenant?> GetByCodeAsync(string code)
    {
        lock (_lock)
        {
            var tenant = _tenants.Values.FirstOrDefault(t => t.Code == code);
            return Task.FromResult(tenant);
        }
    }

    public Task<IEnumerable<Tenant>> GetAllAsync(bool includeInactive = false)
    {
        lock (_lock)
        {
            var query = _tenants.Values.AsEnumerable();
            if (!includeInactive)
                query = query.Where(t => t.IsActive);
            
            return Task.FromResult(query);
        }
    }

    public Task<Tenant> AddAsync(Tenant tenant)
    {
        lock (_lock)
        {
            _tenants[tenant.Id] = tenant;
            _logger.LogInformation("Tenant added: {TenantId} - {TenantName}", tenant.Id, tenant.Name);
            return Task.FromResult(tenant);
        }
    }

    public Task UpdateAsync(Tenant tenant)
    {
        lock (_lock)
        {
            if (_tenants.ContainsKey(tenant.Id))
            {
                _tenants[tenant.Id] = tenant;
                _logger.LogInformation("Tenant updated: {TenantId} - {TenantName}", tenant.Id, tenant.Name);
            }
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            if (_tenants.ContainsKey(id))
            {
                _tenants.Remove(id);
                _logger.LogInformation("Tenant deleted: {TenantId}", id);
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsAsync(string code)
    {
        lock (_lock)
        {
            return Task.FromResult(_tenants.Values.Any(t => t.Code == code));
        }
    }
}
