namespace CorraStudio.Licensing.Models;

public class LicenseInfo
{
    public string LicenseKey { get; set; } = string.Empty;
    public string LicenseCode { get; set; } = string.Empty;
    public LicenseStatus Status { get; set; }
    public string Plan { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public string? DeviceId { get; set; }
    public int MaxDevices { get; set; } = 1;
    public List<string> Features { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum LicenseStatus
{
    Active = 0,
    Inactive = 1,
    Expired = 2,
    Suspended = 3,
    Revoked = 4,
    Invalid = 5,
    Trial = 6
}

public class LicenseValidationRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, string> HardwareInfo { get; set; } = new();
}

public class LicenseValidationResponse
{
    public bool IsValid { get; set; }
    public LicenseInfo? License { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequireActivation { get; set; }
    public string? ActivationUrl { get; set; }
}

public class LicenseActivationRequest
{
    public string LicenseCode { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class HardwareFingerprint
{
    public string CpuId { get; set; } = string.Empty;
    public string MotherboardSerial { get; set; } = string.Empty;
    public string DiskSerial { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string WindowsProductId { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    
    public string GetFingerprint()
    {
        var combined = $"{CpuId}|{MotherboardSerial}|{DiskSerial}|{MacAddress}|{WindowsProductId}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(combined);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}

public class MayarConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://api.mayar.id/v1";
    public bool IsProduction { get; set; } = false;
    public int ValidationIntervalHours { get; set; } = 24;
    public bool EnableHardwareLock { get; set; } = true;
}
