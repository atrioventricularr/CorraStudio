using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CorraStudio.Deployment.Kiosk;

public interface IKioskMode
{
    bool IsKioskModeEnabled { get; }
    Task EnableKioskModeAsync();
    Task DisableKioskModeAsync();
    Task SetAutoStartAsync(bool enabled);
    bool IsAutoStartEnabled();
    Task SetTouchOptimizedAsync(bool enabled);
}

public class KioskMode : IKioskMode
{
    private readonly ILogger<KioskMode>? _logger;
    private const string RegistryRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "CorraStudio";

    public bool IsKioskModeEnabled { get; private set; }

    public KioskMode(ILogger<KioskMode>? logger = null)
    {
        _logger = logger;
        IsKioskModeEnabled = CheckKioskMode();
    }

    public async Task EnableKioskModeAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                // Set registry for shell replacement (optional)
                // For now, just enable fullscreen mode
                IsKioskModeEnabled = true;
                SaveKioskSetting(true);
                _logger?.LogInformation("Kiosk mode enabled");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to enable kiosk mode");
            }
        });
    }

    public async Task DisableKioskModeAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                IsKioskModeEnabled = false;
                SaveKioskSetting(false);
                _logger?.LogInformation("Kiosk mode disabled");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to disable kiosk mode");
            }
        });
    }

    public async Task SetAutoStartAsync(bool enabled)
    {
        await Task.Run(() =>
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true);
                if (key == null) return;
                
                var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                
                if (enabled)
                {
                    key.SetValue(AppName, $"\"{appPath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
                
                _logger?.LogInformation($"Auto-start {(enabled ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to set auto-start");
            }
        });
    }

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task SetTouchOptimizedAsync(bool enabled)
    {
        await Task.Run(() =>
        {
            // Set touch optimization settings
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CorraStudio", "touch.dat");
            
            File.WriteAllText(settingsPath, enabled ? "enabled" : "disabled");
            _logger?.LogInformation($"Touch optimization {(enabled ? "enabled" : "disabled")}");
        });
    }

    private bool CheckKioskMode()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CorraStudio", "kiosk.dat");
        
        return File.Exists(settingsPath) && File.ReadAllText(settingsPath) == "enabled";
    }

    private void SaveKioskSetting(bool enabled)
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CorraStudio", "kiosk.dat");
        
        var directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        
        File.WriteAllText(settingsPath, enabled ? "enabled" : "disabled");
    }
}
