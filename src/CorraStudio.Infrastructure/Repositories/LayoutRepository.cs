using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Infrastructure.Repositories;

public class LayoutRepository : ILayoutRepository
{
    private readonly ILogger<LayoutRepository> _logger;
    private static readonly Dictionary<Guid, Layout> _layouts = new();
    private static readonly object _lock = new();

    public LayoutRepository(ILogger<LayoutRepository> logger)
    {
        _logger = logger;
    }

    public Task<Layout?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            _layouts.TryGetValue(id, out var layout);
            return Task.FromResult(layout);
        }
    }

    public Task<IEnumerable<Layout>> GetByTenantAsync(Guid tenantId, bool onlyActive = true)
    {
        lock (_lock)
        {
            var query = _layouts.Values.Where(l => l.TenantId == tenantId && !l.IsDeleted);
            if (onlyActive)
                query = query.Where(l => l.IsActive);
            
            return Task.FromResult(query.OrderBy(l => l.DisplayOrder));
        }
    }

    public Task<Layout> AddAsync(Layout layout)
    {
        lock (_lock)
        {
            _layouts[layout.Id] = layout;
            _logger.LogInformation("Layout added: {LayoutId} - {LayoutName}", layout.Id, layout.Name);
            return Task.FromResult(layout);
        }
    }

    public Task UpdateAsync(Layout layout)
    {
        lock (_lock)
        {
            if (_layouts.ContainsKey(layout.Id))
            {
                _layouts[layout.Id] = layout;
                _logger.LogInformation("Layout updated: {LayoutId} - {LayoutName}", layout.Id, layout.Name);
            }
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            if (_layouts.ContainsKey(id))
            {
                _layouts.Remove(id);
                _logger.LogInformation("Layout deleted: {LayoutId}", id);
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsByNameAsync(Guid tenantId, string name)
    {
        lock (_lock)
        {
            return Task.FromResult(_layouts.Values.Any(l => l.TenantId == tenantId && l.Name == name && !l.IsDeleted));
        }
    }
}
