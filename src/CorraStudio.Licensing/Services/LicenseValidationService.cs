using CorraStudio.Licensing.Models;
using CorraStudio.Licensing.Mayar;

namespace CorraStudio.Licensing.Services;

public interface ILicenseValidationService
{
    event EventHandler<LicenseStatus>? LicenseStatusChanged;
    
    Task<bool> InitializeAsync();
    Task<bool> ValidateCurrentLicenseAsync();
    Task<bool> ActivateLicenseAsync(string licenseCode, string email);
    Task<bool> DeactivateLicenseAsync();
    LicenseInfo? GetCurrentLicense();
    bool IsFeatureEnabled(string featureName);
    bool IsLicenseValid { get; }
    LicenseStatus CurrentStatus { get; }
    Task<bool> ShowActivationDialogAsync();
}

public class LicenseValidationService : ILicenseValidationService
{
    private readonly IMayarLicenseService _mayarService;
    private readonly ILogger<LicenseValidationService>? _logger;
    private LicenseInfo? _currentLicense;
    private Timer? _validationTimer;
    private bool _isInitialized;

    public event EventHandler<LicenseStatus>? LicenseStatusChanged;

    public bool IsLicenseValid => _currentLicense != null && _currentLicense.Status == LicenseStatus.Active;
    public LicenseStatus CurrentStatus => _currentLicense?.Status ?? LicenseStatus.Invalid;
    public LicenseInfo? GetCurrentLicense() => _currentLicense;

    public LicenseValidationService(IMayarLicenseService mayarService, ILogger<LicenseValidationService>? logger = null)
    {
        _mayarService = mayarService;
        _logger = logger;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            // Load cached license
            _currentLicense = await LoadCachedLicense();
            
            // Validate license
            await ValidateCurrentLicenseAsync();
            
            // Start periodic validation (every 24 hours)
            _validationTimer = new Timer(async _ => await ValidateCurrentLicenseAsync(), 
                null, TimeSpan.FromHours(24), TimeSpan.FromHours(24));
            
            _isInitialized = true;
            return IsLicenseValid;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "License initialization failed");
            return false;
        }
    }

    public async Task<bool> ValidateCurrentLicenseAsync()
    {
        if (_currentLicense == null)
            return false;
        
        var result = await _mayarService.ValidateLicenseAsync(_currentLicense.LicenseKey);
        
        if (result.IsValid)
        {
            _currentLicense = result.License;
            LicenseStatusChanged?.Invoke(this, _currentLicense.Status);
            return true;
        }
        
        _currentLicense = null;
        LicenseStatusChanged?.Invoke(this, LicenseStatus.Invalid);
        return false;
    }

    public async Task<bool> ActivateLicenseAsync(string licenseCode, string email)
    {
        var request = new LicenseActivationRequest
        {
            LicenseCode = licenseCode,
            DeviceId = Environment.MachineName,
            DeviceName = Environment.MachineName,
            Email = email
        };
        
        var result = await _mayarService.ActivateLicenseAsync(request);
        
        if (result.IsValid)
        {
            _currentLicense = result.License;
            LicenseStatusChanged?.Invoke(this, _currentLicense.Status);
            return true;
        }
        
        return false;
    }

    public async Task<bool> DeactivateLicenseAsync()
    {
        if (_currentLicense == null)
            return false;
        
        var hardwareFingerprint = GetHardwareFingerprint();
        var result = await _mayarService.DeactivateLicenseAsync(
            _currentLicense.LicenseKey, 
            hardwareFingerprint.GetFingerprint());
        
        if (result)
        {
            _currentLicense = null;
            LicenseStatusChanged?.Invoke(this, LicenseStatus.Inactive);
        }
        
        return result;
    }

    public bool IsFeatureEnabled(string featureName)
    {
        if (_currentLicense == null)
            return false;
        
        return _currentLicense.Features.Contains(featureName) ||
               _currentLicense.Features.Contains("all") ||
               _currentLicense.Plan == "Enterprise";
    }

    public async Task<bool> ShowActivationDialogAsync()
    {
        // This would show a modal dialog
        // For now, return false
        return await Task.FromResult(false);
    }

    #region Private Methods

    private async Task<LicenseInfo?> LoadCachedLicense()
    {
        try
        {
            var licensePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CorraStudio", "license.dat");
            
            if (File.Exists(licensePath))
            {
                var encrypted = await File.ReadAllTextAsync(licensePath);
                var data = DecryptLicenseData(encrypted);
                return JsonSerializer.Deserialize<LicenseInfo>(data);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load cached license");
        }
        
        return null;
    }

    private string DecryptLicenseData(string encryptedData)
    {
        try
        {
            var bytes = Convert.FromBase64String(encryptedData);
            var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                bytes,
                Encoding.UTF8.GetBytes("CorraStudio"),
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return encryptedData;
        }
    }

    private HardwareFingerprint GetHardwareFingerprint()
    {
        var fingerprint = new HardwareFingerprint();
        
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                fingerprint.CpuId = obj["ProcessorId"]?.ToString() ?? "";
                break;
            }
            
            using var diskSearcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0");
            foreach (var obj in diskSearcher.Get())
            {
                fingerprint.DiskSerial = obj["SerialNumber"]?.ToString() ?? "";
                break;
            }
            
            using var macSearcher = new System.Management.ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True");
            foreach (var obj in macSearcher.Get())
            {
                fingerprint.MacAddress = obj["MACAddress"]?.ToString() ?? "";
                break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get hardware fingerprint");
        }
        
        return fingerprint;
    }

    #endregion
}
