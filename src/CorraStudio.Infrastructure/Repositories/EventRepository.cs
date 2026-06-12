using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ILogger<EventRepository> _logger;
    private static readonly Dictionary<Guid, Event> _events = new();
    private static readonly object _lock = new();

    public EventRepository(ILogger<EventRepository> logger)
    {
        _logger = logger;
    }

    public Task<Event?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            _events.TryGetValue(id, out var eventEntity);
            return Task.FromResult(eventEntity);
        }
    }

    public Task<IEnumerable<Event>> GetByTenantAsync(Guid tenantId, bool onlyActive = true)
    {
        lock (_lock)
        {
            var query = _events.Values.Where(e => e.TenantId == tenantId && !e.IsDeleted);
            if (onlyActive)
                query = query.Where(e => e.IsActive);
            
            return Task.FromResult(query.OrderBy(e => e.StartDate));
        }
    }

    public Task<Event?> GetCurrentActiveEventAsync(Guid tenantId)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var activeEvent = _events.Values.FirstOrDefault(e => 
                e.TenantId == tenantId && 
                e.IsActive && 
                !e.IsDeleted && 
                now >= e.StartDate && 
                now <= e.EndDate);
            return Task.FromResult(activeEvent);
        }
    }

    public Task<Event> AddAsync(Event eventEntity)
    {
        lock (_lock)
        {
            _events[eventEntity.Id] = eventEntity;
            _logger.LogInformation("Event added: {EventId} - {EventName}", eventEntity.Id, eventEntity.Name);
            return Task.FromResult(eventEntity);
        }
    }

    public Task UpdateAsync(Event eventEntity)
    {
        lock (_lock)
        {
            if (_events.ContainsKey(eventEntity.Id))
            {
                _events[eventEntity.Id] = eventEntity;
                _logger.LogInformation("Event updated: {EventId} - {EventName}", eventEntity.Id, eventEntity.Name);
            }
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            if (_events.ContainsKey(id))
            {
                _events.Remove(id);
                _logger.LogInformation("Event deleted: {EventId}", id);
            }
            return Task.CompletedTask;
        }
    }
}
