using System.Text;
using System.Text.Json;
using CorraStudio.Licensing.Models;

namespace CorraStudio.Licensing.Mayar;

public interface IMayarLicenseService
{
    Task<LicenseValidationResponse> ValidateLicenseAsync(string licenseKey);
    Task<LicenseValidationResponse> ActivateLicenseAsync(LicenseActivationRequest request);
    Task<bool> DeactivateLicenseAsync(string licenseKey, string deviceId);
    Task<LicenseInfo?> GetLicenseInfoAsync(string licenseKey);
    Task<bool> CheckFeatureAsync(string licenseKey, string featureName);
    Task<bool> IsLicenseValidAsync(string licenseKey);
    Task<LicenseInfo?> GetCachedLicenseAsync();
    Task InvalidateCacheAsync();
}

public class MayarLicenseService : IMayarLicenseService
{
    private readonly MayarConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MayarLicenseService>? _logger;
    private LicenseInfo? _cachedLicense;
    private DateTime? _lastValidation;
    private readonly object _lock = new();

    public MayarLicenseService(MayarConfig config, ILogger<MayarLicenseService>? logger = null)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CorraStudio-Photobooth/1.0");
    }

    public async Task<LicenseValidationResponse> ValidateLicenseAsync(string licenseKey)
    {
        try
        {
            var hardwareFingerprint = GetHardwareFingerprint();
            var request = new LicenseValidationRequest
            {
                LicenseKey = licenseKey,
                DeviceId = hardwareFingerprint.GetFingerprint(),
                DeviceName = Environment.MachineName,
                Version = "1.0.0",
                HardwareInfo = new Dictionary<string, string>
                {
                    ["cpu"] = hardwareFingerprint.CpuId,
                    ["disk"] = hardwareFingerprint.DiskSerial,
                    ["mac"] = hardwareFingerprint.MacAddress
                }
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_config.ApiUrl}/license/validate", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<MayarValidationResponse>(responseContent);
                
                if (result != null && result.Success)
                {
                    var license = MapToLicenseInfo(result);
                    
                    lock (_lock)
                    {
                        _cachedLicense = license;
                        _lastValidation = DateTime.UtcNow;
                        SaveLicenseToLocal(license);
                    }
                    
                    return new LicenseValidationResponse
                    {
                        IsValid = true,
                        License = license
                    };
                }
                
                return new LicenseValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = result?.Message ?? "Invalid license"
                };
            }
            
            return new LicenseValidationResponse
            {
                IsValid = false,
                ErrorMessage = $"API error: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "License validation failed");
            
            // Try to use cached license if available
            if (_cachedLicense != null && _lastValidation.HasValue)
            {
                var hoursSinceValidation = (DateTime.UtcNow - _lastValidation.Value).TotalHours;
                if (hoursSinceValidation < _config.ValidationIntervalHours)
                {
                    return new LicenseValidationResponse
                    {
                        IsValid = true,
                        License = _cachedLicense
                    };
                }
            }
            
            return new LicenseValidationResponse
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<LicenseValidationResponse> ActivateLicenseAsync(LicenseActivationRequest request)
    {
        try
        {
            var hardwareFingerprint = GetHardwareFingerprint();
            var activationRequest = new
            {
                license_code = request.LicenseCode,
                device_id = hardwareFingerprint.GetFingerprint(),
                device_name = request.DeviceName,
                email = request.Email,
                hardware_info = new
                {
                    cpu = hardwareFingerprint.CpuId,
                    motherboard = hardwareFingerprint.MotherboardSerial,
                    disk = hardwareFingerprint.DiskSerial,
                    mac = hardwareFingerprint.MacAddress
                }
            };
            
            var json = JsonSerializer.Serialize(activationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_config.ApiUrl}/license/activate", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<MayarActivationResponse>(responseContent);
                
                if (result != null && result.Success)
                {
                    var license = new LicenseInfo
                    {
                        LicenseKey = result.LicenseKey ?? "",
                        LicenseCode = request.LicenseCode,
                        Status = LicenseStatus.Active,
                        Plan = result.Plan ?? "Professional",
                        IssuedAt = DateTime.UtcNow,
                        ExpiresAt = result.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                        DeviceId = hardwareFingerprint.GetFingerprint(),
                        Features = result.Features ?? new List<string>(),
                        Metadata = result.Metadata ?? new Dictionary<string, object>()
                    };
                    
                    lock (_lock)
                    {
                        _cachedLicense = license;
                        _lastValidation = DateTime.UtcNow;
                        SaveLicenseToLocal(license);
                    }
                    
                    return new LicenseValidationResponse
                    {
                        IsValid = true,
                        License = license
                    };
                }
                
                return new LicenseValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = result?.Message ?? "Activation failed"
                };
            }
            
            return new LicenseValidationResponse
            {
                IsValid = false,
                ErrorMessage = await response.Content.ReadAsStringAsync()
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "License activation failed");
            return new LicenseValidationResponse
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> DeactivateLicenseAsync(string licenseKey, string deviceId)
    {
        try
        {
            var request = new { license_key = licenseKey, device_id = deviceId };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_config.ApiUrl}/license/deactivate", content);
            
            if (response.IsSuccessStatusCode)
            {
                lock (_lock)
                {
                    _cachedLicense = null;
                    _lastValidation = null;
                    ClearLocalLicense();
                }
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "License deactivation failed");
            return false;
        }
    }

    public async Task<LicenseInfo?> GetLicenseInfoAsync(string licenseKey)
    {
        var result = await ValidateLicenseAsync(licenseKey);
        return result.IsValid ? result.License : null;
    }

    public async Task<bool> CheckFeatureAsync(string licenseKey, string featureName)
    {
        var license = await GetLicenseInfoAsync(licenseKey);
        if (license == null) return false;
        
        return license.Features.Contains(featureName) || 
               license.Features.Contains("all") ||
               license.Plan == "Enterprise";
    }

    public async Task<bool> IsLicenseValidAsync(string licenseKey)
    {
        var result = await ValidateLicenseAsync(licenseKey);
        return result.IsValid && result.License?.Status == LicenseStatus.Active;
    }

    public async Task<LicenseInfo?> GetCachedLicenseAsync()
    {
        return await Task.FromResult(_cachedLicense);
    }

    public async Task InvalidateCacheAsync()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                _cachedLicense = null;
                _lastValidation = null;
            }
        });
    }

    #region Private Methods

    private HardwareFingerprint GetHardwareFingerprint()
    {
        var fingerprint = new HardwareFingerprint();
        
        try
        {
            // Get CPU ID
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                fingerprint.CpuId = obj["ProcessorId"]?.ToString() ?? "";
                break;
            }
            
            // Get Motherboard Serial
            using var mbSearcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (var obj in mbSearcher.Get())
            {
                fingerprint.MotherboardSerial = obj["SerialNumber"]?.ToString() ?? "";
                break;
            }
            
            // Get Disk Serial
            using var diskSearcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0");
            foreach (var obj in diskSearcher.Get())
            {
                fingerprint.DiskSerial = obj["SerialNumber"]?.ToString() ?? "";
                break;
            }
            
            // Get MAC Address
            using var macSearcher = new System.Management.ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True");
            foreach (var obj in macSearcher.Get())
            {
                fingerprint.MacAddress = obj["MACAddress"]?.ToString() ?? "";
                break;
            }
            
            fingerprint.ComputerName = Environment.MachineName;
            fingerprint.UserName = Environment.UserName;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get hardware fingerprint");
        }
        
        return fingerprint;
    }

    private LicenseInfo MapToLicenseInfo(MayarValidationResponse response)
    {
        return new LicenseInfo
        {
            LicenseKey = response.LicenseKey ?? "",
            LicenseCode = response.LicenseCode ?? "",
            Status = response.Status == "active" ? LicenseStatus.Active : LicenseStatus.Inactive,
            Plan = response.Plan ?? "Basic",
            IssuedAt = response.IssuedAt ?? DateTime.UtcNow,
            ExpiresAt = response.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
            LastValidatedAt = DateTime.UtcNow,
            DeviceId = response.DeviceId,
            MaxDevices = response.MaxDevices ?? 1,
            Features = response.Features ?? new List<string>(),
            Metadata = response.Metadata ?? new Dictionary<string, object>()
        };
    }

    private void SaveLicenseToLocal(LicenseInfo license)
    {
        try
        {
            var licensePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CorraStudio", "license.dat");
            
            var directory = Path.GetDirectoryName(licensePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var encrypted = EncryptLicenseData(JsonSerializer.Serialize(license));
            File.WriteAllText(licensePath, encrypted);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save license locally");
        }
    }

    private void ClearLocalLicense()
    {
        try
        {
            var licensePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CorraStudio", "license.dat");
            
            if (File.Exists(licensePath))
            {
                File.Delete(licensePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear local license");
        }
    }

    private string EncryptLicenseData(string data)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var protectedBytes = System.Security.Cryptography.ProtectedData.Protect(
                bytes, 
                Encoding.UTF8.GetBytes("CorraStudio"), 
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }
        catch
        {
            return data;
        }
    }

    #endregion
}

#region Mayar API Response Models

internal class MayarValidationResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? LicenseKey { get; set; }
    public string? LicenseCode { get; set; }
    public string? Status { get; set; }
    public string? Plan { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? DeviceId { get; set; }
    public int? MaxDevices { get; set; }
    public List<string>? Features { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

internal class MayarActivationResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? LicenseKey { get; set; }
    public string? Plan { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string>? Features { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

#endregion
