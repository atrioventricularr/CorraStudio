using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Infrastructure.Repositories;

public class PhotoRepository : IPhotoRepository
{
    private readonly ILogger<PhotoRepository> _logger;
    private static readonly Dictionary<Guid, Photo> _photos = new();
    private static readonly object _lock = new();

    public PhotoRepository(ILogger<PhotoRepository> logger)
    {
        _logger = logger;
    }

    public Task<Photo?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            _photos.TryGetValue(id, out var photo);
            return Task.FromResult(photo);
        }
    }

    public Task<IEnumerable<Photo>> GetBySessionAsync(Guid sessionId)
    {
        lock (_lock)
        {
            var photos = _photos.Values.Where(p => p.SessionId == sessionId && !p.IsDeleted).OrderBy(p => p.OrderIndex);
            return Task.FromResult(photos);
        }
    }

    public Task<IEnumerable<Photo>> GetSelectedBySessionAsync(Guid sessionId)
    {
        lock (_lock)
        {
            var photos = _photos.Values.Where(p => p.SessionId == sessionId && p.IsSelected && !p.IsDeleted).OrderBy(p => p.OrderIndex);
            return Task.FromResult(photos);
        }
    }

    public Task<Photo> AddAsync(Photo photo)
    {
        lock (_lock)
        {
            _photos[photo.Id] = photo;
            _logger.LogInformation("Photo added: {PhotoId} - Session: {SessionId}", photo.Id, photo.SessionId);
            return Task.FromResult(photo);
        }
    }

    public Task UpdateAsync(Photo photo)
    {
        lock (_lock)
        {
            if (_photos.ContainsKey(photo.Id))
            {
                _photos[photo.Id] = photo;
                _logger.LogInformation("Photo updated: {PhotoId}", photo.Id);
            }
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            if (_photos.ContainsKey(id))
            {
                _photos.Remove(id);
                _logger.LogInformation("Photo deleted: {PhotoId}", id);
            }
            return Task.CompletedTask;
        }
    }

    public Task DeleteBySessionAsync(Guid sessionId)
    {
        lock (_lock)
        {
            var photosToDelete = _photos.Values.Where(p => p.SessionId == sessionId).ToList();
            foreach (var photo in photosToDelete)
            {
                _photos.Remove(photo.Id);
            }
            _logger.LogInformation("All photos deleted for session: {SessionId}", sessionId);
            return Task.CompletedTask;
        }
    }
}
