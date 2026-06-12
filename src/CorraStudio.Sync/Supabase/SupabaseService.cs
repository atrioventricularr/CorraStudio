using System.Text;
using System.Text.Json;
using CorraStudio.Sync.Models;

namespace CorraStudio.Sync.Supabase;

public interface ISupabaseService
{
    Task<bool> InitializeAsync();
    Task<UploadResult> UploadPhotoAsync(byte[] photoData, string sessionId, int orderIndex);
    Task<GalleryTokenResult> CreateGallerySessionAsync(Guid sessionId, string sessionCode, string? customerEmail = null);
    Task<List<GalleryPhoto>> GetSessionPhotosAsync(string accessToken);
    Task<bool> DeleteSessionAsync(string accessToken);
    Task<bool> IsHealthyAsync();
}

public class SupabaseService : ISupabaseService
{
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    private readonly string _bucketName;
    private readonly ILogger<SupabaseService>? _logger;
    private HttpClient? _httpClient;
    private bool _isInitialized;

    public SupabaseService(ILogger<SupabaseService>? logger = null)
    {
        _logger = logger;
        _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
        _supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";
        _bucketName = Environment.GetEnvironmentVariable("SUPABASE_BUCKET") ?? "corra-gallery";
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseKey))
            {
                _logger?.LogWarning("Supabase credentials not configured");
                return false;
            }

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
            
            // Test connection
            var response = await _httpClient.GetAsync($"{_supabaseUrl}/rest/v1/health");
            _isInitialized = response.IsSuccessStatusCode;
            
            if (_isInitialized)
            {
                await EnsureBucketExists();
                await EnsureTablesExist();
            }
            
            return _isInitialized;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize Supabase");
            return false;
        }
    }

    public async Task<UploadResult> UploadPhotoAsync(byte[] photoData, string sessionId, int orderIndex)
    {
        if (!_isInitialized)
            return new UploadResult { Success = false, ErrorMessage = "Supabase not initialized" };

        try
        {
            var fileName = $"{sessionId}/photo_{orderIndex}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var content = new ByteArrayContent(photoData);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            
            var url = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{fileName}";
            var response = await _httpClient!.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var publicUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{fileName}";
                
                // Save photo metadata to database
                await SavePhotoMetadata(sessionId, fileName, publicUrl, orderIndex, photoData.Length);
                
                return new UploadResult
                {
                    Success = true,
                    PublicUrl = publicUrl,
                    StoragePath = fileName
                };
            }
            
            var error = await response.Content.ReadAsStringAsync();
            return new UploadResult { Success = false, ErrorMessage = error };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Upload failed");
            return new UploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<GalleryTokenResult> CreateGallerySessionAsync(Guid sessionId, string sessionCode, string? customerEmail = null)
    {
        if (!_isInitialized)
            return new GalleryTokenResult { Success = false, ErrorMessage = "Supabase not initialized" };

        try
        {
            var token = GenerateAccessToken();
            var expiresAt = DateTime.UtcNow.AddDays(30); // 30 days expiry
            
            var sessionData = new
            {
                id = sessionId,
                session_code = sessionCode,
                access_token = token,
                customer_email = customerEmail,
                expires_at = expiresAt,
                created_at = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(sessionData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient!.PostAsync($"{_supabaseUrl}/rest/v1/gallery_sessions", content);
            
            if (response.IsSuccessStatusCode)
            {
                var galleryUrl = Environment.GetEnvironmentVariable("GALLERY_URL") ?? "https://corra-gallery.netlify.app";
                var fullUrl = $"{galleryUrl}/gallery/{token}";
                
                return new GalleryTokenResult
                {
                    Success = true,
                    Token = token,
                    GalleryUrl = fullUrl
                };
            }
            
            return new GalleryTokenResult { Success = false, ErrorMessage = await response.Content.ReadAsStringAsync() };
        }
        catch (Exception ex)
        {
            return new GalleryTokenResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<List<GalleryPhoto>> GetSessionPhotosAsync(string accessToken)
    {
        var photos = new List<GalleryPhoto>();
        
        if (!_isInitialized)
            return photos;

        try
        {
            // Get session ID from token
            var sessionResponse = await _httpClient!.GetAsync($"{_supabaseUrl}/rest/v1/gallery_sessions?access_token=eq.{accessToken}");
            var sessions = JsonSerializer.Deserialize<List<dynamic>>(await sessionResponse.Content.ReadAsStringAsync());
            
            if (sessions == null || sessions.Count == 0)
                return photos;
            
            var sessionId = sessions[0].id.ToString();
            
            // Get photos for session
            var photosResponse = await _httpClient!.GetAsync($"{_supabaseUrl}/rest/v1/gallery_photos?session_id=eq.{sessionId}&order=order_index.asc");
            var photosData = JsonSerializer.Deserialize<List<GalleryPhoto>>(await photosResponse.Content.ReadAsStringAsync());
            
            return photosData ?? photos;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get session photos");
            return photos;
        }
    }

    public async Task<bool> DeleteSessionAsync(string accessToken)
    {
        if (!_isInitialized)
            return false;

        try
        {
            // Delete photos first, then session
            var response = await _httpClient!.DeleteAsync($"{_supabaseUrl}/rest/v1/gallery_sessions?access_token=eq.{accessToken}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete session");
            return false;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        if (!_isInitialized)
            return false;
        
        try
        {
            var response = await _httpClient!.GetAsync($"{_supabaseUrl}/rest/v1/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #region Private Methods

    private async Task EnsureBucketExists()
    {
        try
        {
            var response = await _httpClient!.GetAsync($"{_supabaseUrl}/storage/v1/bucket/{_bucketName}");
            if (!response.IsSuccessStatusCode)
            {
                var bucketData = new { name = _bucketName, public = true };
                var json = JsonSerializer.Serialize(bucketData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync($"{_supabaseUrl}/storage/v1/bucket", content);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to ensure bucket exists");
        }
    }

    private async Task EnsureTablesExist()
    {
        // Create tables if they don't exist
        // This would be handled by Supabase migrations in production
        await Task.CompletedTask;
    }

    private async Task SavePhotoMetadata(string sessionId, string fileName, string publicUrl, int orderIndex, long fileSize)
    {
        try
        {
            var photoData = new
            {
                session_id = Guid.Parse(sessionId.Split('/')[0]),
                file_name = fileName,
                storage_path = fileName,
                public_url = publicUrl,
                order_index = orderIndex,
                file_size_bytes = fileSize,
                created_at = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(photoData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient!.PostAsync($"{_supabaseUrl}/rest/v1/gallery_photos", content);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save photo metadata");
        }
    }

    private string GenerateAccessToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
    }

    #endregion
}
