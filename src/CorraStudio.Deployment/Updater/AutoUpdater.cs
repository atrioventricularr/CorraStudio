using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace CorraStudio.Deployment.Updater;

public interface IAutoUpdater
{
    event EventHandler<UpdateProgress>? ProgressChanged;
    event EventHandler<UpdateInfo>? UpdateAvailable;
    event EventHandler<bool>? UpdateCompleted;
    
    Task<UpdateCheckResult> CheckForUpdatesAsync();
    Task<bool> DownloadUpdateAsync(UpdateInfo update);
    Task<bool> InstallUpdateAsync();
    Task<bool> RollbackUpdateAsync();
    string GetCurrentVersion();
    bool IsUpdatePending { get; }
}

public class AutoUpdater : IAutoUpdater
{
    private readonly string _updateUrl;
    private readonly string _appPath;
    private readonly string _updatePath;
    private UpdateInfo? _pendingUpdate;
    private readonly ILogger<AutoUpdater>? _logger;
    private readonly HttpClient _httpClient;

    public event EventHandler<UpdateProgress>? ProgressChanged;
    public event EventHandler<UpdateInfo>? UpdateAvailable;
    public event EventHandler<bool>? UpdateCompleted;

    public bool IsUpdatePending => _pendingUpdate != null;

    public AutoUpdater(string updateUrl, ILogger<AutoUpdater>? logger = null)
    {
        _updateUrl = updateUrl;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
        
        _appPath = AppDomain.CurrentDomain.BaseDirectory;
        _updatePath = Path.Combine(Path.GetTempPath(), "CorraStudio_Updates");
        
        if (!Directory.Exists(_updatePath))
            Directory.CreateDirectory(_updatePath);
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            var updateInfoUrl = $"{_updateUrl}/update-info.json";
            
            var response = await _httpClient.GetAsync(updateInfoUrl);
            if (!response.IsSuccessStatusCode)
                return new UpdateCheckResult { HasUpdate = false, ErrorMessage = "Failed to check updates" };
            
            var json = await response.Content.ReadAsStringAsync();
            var latestVersion = JsonSerializer.Deserialize<UpdateInfo>(json);
            
            if (latestVersion == null)
                return new UpdateCheckResult { HasUpdate = false };
            
            var hasUpdate = IsNewerVersion(currentVersion, latestVersion.Version);
            
            if (hasUpdate)
            {
                UpdateAvailable?.Invoke(this, latestVersion);
            }
            
            return new UpdateCheckResult
            {
                HasUpdate = hasUpdate,
                LatestVersion = latestVersion
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check for updates");
            return new UpdateCheckResult { HasUpdate = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<bool> DownloadUpdateAsync(UpdateInfo update)
    {
        try
        {
            var progress = new UpdateProgress { Status = UpdateStatus.Downloading };
            
            using var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var updateZipPath = Path.Combine(_updatePath, $"update_{update.Version}.zip");
            
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(updateZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            
            var buffer = new byte[8192];
            long bytesRead = 0;
            int bytes;
            
            while ((bytes = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytes));
                bytesRead += bytes;
                
                progress.ProgressPercentage = (int)(bytesRead * 100 / totalBytes);
                progress.BytesDownloaded = bytesRead;
                progress.TotalBytes = totalBytes;
                ProgressChanged?.Invoke(this, progress);
            }
            
            await fileStream.FlushAsync();
            
            // Verify hash
            var fileHash = await CalculateFileHashAsync(updateZipPath);
            if (!fileHash.Equals(update.FileHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("File hash mismatch");
            }
            
            // Extract update
            var extractPath = Path.Combine(_updatePath, update.Version);
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);
            Directory.CreateDirectory(extractPath);
            
            ZipFile.ExtractToDirectory(updateZipPath, extractPath);
            
            _pendingUpdate = update;
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download update");
            return false;
        }
    }

    public async Task<bool> InstallUpdateAsync()
    {
        if (_pendingUpdate == null)
            return false;
        
        try
        {
            var progress = new UpdateProgress { Status = UpdateStatus.Installing, ProgressPercentage = 0 };
            
            var updatePath = Path.Combine(_updatePath, _pendingUpdate.Version);
            var backupPath = Path.Combine(_updatePath, "backup");
            
            // Create backup
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, true);
            Directory.CreateDirectory(backupPath);
            
            progress.ProgressPercentage = 20;
            ProgressChanged?.Invoke(this, progress);
            
            // Backup current files
            foreach (var file in Directory.GetFiles(_appPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(file);
                var backupFile = Path.Combine(backupPath, fileName);
                File.Copy(file, backupFile, true);
            }
            
            progress.ProgressPercentage = 40;
            ProgressChanged?.Invoke(this, progress);
            
            // Copy update files
            foreach (var file in Directory.GetFiles(updatePath, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(updatePath, file);
                var destPath = Path.Combine(_appPath, relativePath);
                var destDir = Path.GetDirectoryName(destPath);
                
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                
                File.Copy(file, destPath, true);
            }
            
            progress.ProgressPercentage = 80;
            ProgressChanged?.Invoke(this, progress);
            
            // Save update info
            var versionFile = Path.Combine(_appPath, "version.json");
            var versionInfo = new { Version = _pendingUpdate.Version, UpdatedAt = DateTime.UtcNow };
            await File.WriteAllTextAsync(versionFile, JsonSerializer.Serialize(versionInfo));
            
            progress.ProgressPercentage = 100;
            ProgressChanged?.Invoke(this, progress);
            
            _pendingUpdate = null;
            UpdateCompleted?.Invoke(this, true);
            
            // Restart application
            RestartApplication();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to install update");
            await RollbackUpdateAsync();
            UpdateCompleted?.Invoke(this, false);
            return false;
        }
    }

    public async Task<bool> RollbackUpdateAsync()
    {
        try
        {
            var backupPath = Path.Combine(_updatePath, "backup");
            if (!Directory.Exists(backupPath))
                return false;
            
            foreach (var file in Directory.GetFiles(backupPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(file);
                var destPath = Path.Combine(_appPath, fileName);
                File.Copy(file, destPath, true);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to rollback update");
            return false;
        }
    }

    public string GetCurrentVersion()
    {
        var versionFile = Path.Combine(_appPath, "version.json");
        if (File.Exists(versionFile))
        {
            try
            {
                var json = File.ReadAllText(versionFile);
                var versionInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return versionInfo?.GetValueOrDefault("Version") ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }
        
        // Get from assembly
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    private bool IsNewerVersion(string current, string latest)
    {
        var currentParts = current.Split('.');
        var latestParts = latest.Split('.');
        
        for (int i = 0; i < Math.Max(currentParts.Length, latestParts.Length); i++)
        {
            var currentPart = i < currentParts.Length ? int.Parse(currentParts[i]) : 0;
            var latestPart = i < latestParts.Length ? int.Parse(latestParts[i]) : 0;
            
            if (latestPart > currentPart) return true;
            if (latestPart < currentPart) return false;
        }
        
        return false;
    }

    private async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void RestartApplication()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName,
            UseShellExecute = true
        };
        
        System.Diagnostics.Process.Start(startInfo);
        Environment.Exit(0);
    }
}
