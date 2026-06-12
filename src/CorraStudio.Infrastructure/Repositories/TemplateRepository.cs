using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Infrastructure.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly ILogger<TemplateRepository> _logger;
    private static readonly Dictionary<Guid, Template> _templates = new();
    private static readonly object _lock = new();

    public TemplateRepository(ILogger<TemplateRepository> logger)
    {
        _logger = logger;
    }

    public Task<Template?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            _templates.TryGetValue(id, out var template);
            return Task.FromResult(template);
        }
    }

    public Task<IEnumerable<Template>> GetByTenantAsync(Guid tenantId, bool onlyActive = true)
    {
        lock (_lock)
        {
            var query = _templates.Values.Where(t => t.TenantId == tenantId && !t.IsDeleted);
            if (onlyActive)
                query = query.Where(t => t.IsActive);
            
            return Task.FromResult(query.OrderBy(t => t.DisplayOrder));
        }
    }

    public Task<Template> AddAsync(Template template)
    {
        lock (_lock)
        {
            _templates[template.Id] = template;
            _logger.LogInformation("Template added: {TemplateId} - {TemplateName}", template.Id, template.Name);
            return Task.FromResult(template);
        }
    }

    public Task UpdateAsync(Template template)
    {
        lock (_lock)
        {
            if (_templates.ContainsKey(template.Id))
            {
                _templates[template.Id] = template;
                _logger.LogInformation("Template updated: {TemplateId} - {TemplateName}", template.Id, template.Name);
            }
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            if (_templates.ContainsKey(id))
            {
                _templates.Remove(id);
                _logger.LogInformation("Template deleted: {TemplateId}", id);
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsByNameAsync(Guid tenantId, string name)
    {
        lock (_lock)
        {
            return Task.FromResult(_templates.Values.Any(t => t.TenantId == tenantId && t.Name == name && !t.IsDeleted));
        }
    }
}
