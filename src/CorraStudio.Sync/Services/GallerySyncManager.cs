using System.Collections.Concurrent;
using CorraStudio.Sync.Models;
using CorraStudio.Sync.Supabase;

namespace CorraStudio.Sync.Services;

public interface IGallerySyncManager
{
    event EventHandler<SyncProgressEventArgs>? SyncProgress;
    event EventHandler<SyncCompletedEventArgs>? SyncCompleted;
    
    Task<bool> InitializeAsync();
    Task<bool> UploadSessionPhotosAsync(Guid sessionId, string sessionCode, List<byte[]> photos, string? customerEmail = null);
    Task<string?> GenerateGalleryQrCodeAsync(Guid sessionId, string sessionCode);
    Task<List<GalleryPhoto>> GetSessionPhotosAsync(string accessToken);
    Task<bool> IsCloudAvailableAsync();
    Task<int> GetPendingSyncCountAsync();
    Task ProcessPendingSyncsAsync();
}

public class SyncProgressEventArgs : EventArgs
{
    public int CurrentPhoto { get; set; }
    public int TotalPhotos { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SyncCompletedEventArgs : EventArgs
{
    public bool Success { get; set; }
    public string GalleryUrl { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class GallerySyncManager : IGallerySyncManager
{
    private readonly ISupabaseService _supabaseService;
    private readonly ConcurrentQueue<PendingSyncItem> _pendingSyncs;
    private readonly ILogger<GallerySyncManager>? _logger;
    private bool _isInitialized;
    private bool _isProcessing;

    public event EventHandler<SyncProgressEventArgs>? SyncProgress;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public GallerySyncManager(ISupabaseService supabaseService, ILogger<GallerySyncManager>? logger = null)
    {
        _supabaseService = supabaseService;
        _logger = logger;
        _pendingSyncs = new ConcurrentQueue<PendingSyncItem>();
    }

    public async Task<bool> InitializeAsync()
    {
        _isInitialized = await _supabaseService.InitializeAsync();
        
        if (_isInitialized)
        {
            // Start background processor
            _ = Task.Run(ProcessPendingSyncsAsync);
        }
        
        return _isInitialized;
    }

    public async Task<bool> UploadSessionPhotosAsync(Guid sessionId, string sessionCode, List<byte[]> photos, string? customerEmail = null)
    {
        if (!_isInitialized)
        {
            // Queue for later sync
            var pendingItem = new PendingSyncItem
            {
                SessionId = sessionId,
                SessionCode = sessionCode,
                Photos = photos,
                CustomerEmail = customerEmail,
                CreatedAt = DateTime.UtcNow
            };
            _pendingSyncs.Enqueue(pendingItem);
            _logger?.LogInformation("Session {SessionCode} queued for sync", sessionCode);
            return true;
        }

        try
        {
            var syncProgress = new SyncProgressEventArgs { TotalPhotos = photos.Count };
            
            // Create gallery session
            var tokenResult = await _supabaseService.CreateGallerySessionAsync(sessionId, sessionCode, customerEmail);
            if (!tokenResult.Success)
                return false;
            
            // Upload each photo
            for (int i = 0; i < photos.Count; i++)
            {
                syncProgress.CurrentPhoto = i + 1;
                syncProgress.Status = $"Uploading photo {i + 1} of {photos.Count}";
                SyncProgress?.Invoke(this, syncProgress);
                
                var uploadResult = await _supabaseService.UploadPhotoAsync(photos[i], sessionId.ToString(), i);
                if (!uploadResult.Success)
                {
                    _logger?.LogWarning("Failed to upload photo {Index} for session {SessionCode}", i, sessionCode);
                }
            }
            
            syncProgress.Status = "Complete!";
            SyncProgress?.Invoke(this, syncProgress);
            
            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs
            {
                Success = true,
                GalleryUrl = tokenResult.GalleryUrl ?? ""
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Upload failed for session {SessionCode}", sessionCode);
            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs
            {
                Success = false,
                ErrorMessage = ex.Message
            });
            return false;
        }
    }

    public async Task<string?> GenerateGalleryQrCodeAsync(Guid sessionId, string sessionCode)
    {
        var tokenResult = await _supabaseService.CreateGallerySessionAsync(sessionId, sessionCode);
        return tokenResult.Success ? tokenResult.GalleryUrl : null;
    }

    public async Task<List<GalleryPhoto>> GetSessionPhotosAsync(string accessToken)
    {
        return await _supabaseService.GetSessionPhotosAsync(accessToken);
    }

    public async Task<bool> IsCloudAvailableAsync()
    {
        return await _supabaseService.IsHealthyAsync();
    }

    public async Task<int> GetPendingSyncCountAsync()
    {
        return await Task.FromResult(_pendingSyncs.Count);
    }

    public async Task ProcessPendingSyncsAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;
        
        while (_pendingSyncs.TryDequeue(out var pendingItem))
        {
            _logger?.LogInformation("Processing pending sync for session {SessionCode}", pendingItem.SessionCode);
            
            // Wait for cloud to be available
            while (!await IsCloudAvailableAsync())
            {
                await Task.Delay(5000);
            }
            
            await UploadSessionPhotosAsync(
                pendingItem.SessionId,
                pendingItem.SessionCode,
                pendingItem.Photos,
                pendingItem.CustomerEmail
            );
        }
        
        _isProcessing = false;
    }
}

internal class PendingSyncItem
{
    public Guid SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public List<byte[]> Photos { get; set; } = new();
    public string? CustomerEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}
