using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CorraStudio.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CorraStudio.Presentation.Wpf.Services;

public interface IThemeService
{
    event EventHandler<ThemeSettings>? ThemeChanged;
    
    Task<ThemeSettings> GetCurrentThemeAsync();
    Task<bool> ApplyThemeAsync(ThemeSettings theme);
    Task<bool> SaveThemeAsync(ThemeSettings theme);
    Task<List<ThemeSettings>> GetAvailableThemesAsync();
    Task<bool> ResetToDefaultAsync();
    Task<bool> SetBrandingAsync(string brandName, string? logoPath = null);
    Task<bool> SetBackgroundAsync(string? imagePath, Color? color = null);
    
    string CurrentThemeId { get; }
    ThemeSettings CurrentTheme { get; }
}

public class ThemeService : IThemeService
{
    private readonly IConfigurationRepository _configRepository;
    private readonly ILogger<ThemeService>? _logger;
    private ThemeSettings _currentTheme;
    private readonly Dictionary<string, ThemeSettings> _availableThemes;
    
    public event EventHandler<ThemeSettings>? ThemeChanged;
    
    public string CurrentThemeId => _currentTheme?.ThemeId ?? "y2k_retro";
    public ThemeSettings CurrentTheme => _currentTheme;

    public ThemeService(IConfigurationRepository configRepository, ILogger<ThemeService>? logger = null)
    {
        _configRepository = configRepository;
        _logger = logger;
        
        _availableThemes = new Dictionary<string, ThemeSettings>
        {
            ["y2k_retro"] = ThemeSettings.Y2KRetro(),
            ["modern_minimal"] = ThemeSettings.ModernMinimal(),
            ["dark_mode"] = ThemeSettings.DarkMode(),
            ["coral_pink"] = ThemeSettings.CoralPink(),
            ["vintage"] = ThemeSettings.Vintage(),
            ["wedding"] = ThemeSettings.Wedding()
        };
        
        _currentTheme = ThemeSettings.Y2KRetro();
        
        // Load saved theme on startup
        Task.Run(async () => await LoadSavedThemeAsync());
    }

    public async Task<ThemeSettings> GetCurrentThemeAsync()
    {
        await LoadSavedThemeAsync();
        return _currentTheme;
    }

    public async Task<bool> ApplyThemeAsync(ThemeSettings theme)
    {
        try
        {
            _currentTheme = theme;
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var resources = Application.Current.Resources;
                
                // Update colors
                resources["PrimaryBrush"] = CreateSolidColorBrush(theme.PrimaryColor);
                resources["SecondaryBrush"] = CreateSolidColorBrush(theme.SecondaryColor);
                resources["AccentBrush"] = CreateSolidColorBrush(theme.AccentColor);
                
                // Update font
                resources["ThemeFontFamily"] = new FontFamily(theme.FontFamily);
                
                // Update brand name
                if (Application.Current.MainWindow?.DataContext is ShellViewModel shell)
                {
                    shell.Title = theme.BrandName;
                }
                
                // Load background if exists
                if (!string.IsNullOrEmpty(theme.BackgroundImage) && File.Exists(theme.BackgroundImage))
                {
                    var image = new BitmapImage(new Uri(theme.BackgroundImage));
                    resources["ThemeBackground"] = new ImageBrush(image);
                }
                else
                {
                    resources["ThemeBackground"] = CreateSolidColorBrush(theme.PrimaryColor);
                }
            });
            
            ThemeChanged?.Invoke(this, theme);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to apply theme");
            return false;
        }
    }

    public async Task<bool> SaveThemeAsync(ThemeSettings theme)
    {
        try
        {
            var tenantId = Guid.Empty; // In production, get current tenant
            
            await _configRepository.SetValueAsync(tenantId, "Theme.Id", theme.ThemeId, "Appearance");
            await _configRepository.SetValueAsync(tenantId, "Theme.BrandName", theme.BrandName, "Appearance");
            await _configRepository.SetValueAsync(tenantId, "Theme.PrimaryColor", theme.PrimaryColor, "Appearance");
            await _configRepository.SetValueAsync(tenantId, "Theme.SecondaryColor", theme.SecondaryColor, "Appearance");
            await _configRepository.SetValueAsync(tenantId, "Theme.AccentColor", theme.AccentColor, "Appearance");
            await _configRepository.SetValueAsync(tenantId, "Theme.FontFamily", theme.FontFamily, "Appearance");
            await _configRepository.SetValueAsync(tenantId, "Theme.BackgroundImage", theme.BackgroundImage ?? "", "Appearance");
            await _configRepository.SetValueAsync(tenantId, "Theme.LogoPath", theme.LogoPath ?? "", "Appearance");
            
            _currentTheme = theme;
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save theme");
            return false;
        }
    }

    public async Task<List<ThemeSettings>> GetAvailableThemesAsync()
    {
        return await Task.FromResult(_availableThemes.Values.ToList());
    }

    public async Task<bool> ResetToDefaultAsync()
    {
        var defaultTheme = ThemeSettings.Y2KRetro();
        await SaveThemeAsync(defaultTheme);
        return await ApplyThemeAsync(defaultTheme);
    }

    public async Task<bool> SetBrandingAsync(string brandName, string? logoPath = null)
    {
        _currentTheme.UpdateBranding(brandName, logoPath);
        return await SaveThemeAsync(_currentTheme);
    }

    public async Task<bool> SetBackgroundAsync(string? imagePath = null, Color? color = null)
    {
        if (imagePath != null && File.Exists(imagePath))
        {
            _currentTheme.UpdateBackground(imagePath);
        }
        else if (color.HasValue)
        {
            _currentTheme.PrimaryColor = color.Value.ToString();
            _currentTheme.UpdateBackground(null);
        }
        
        return await SaveThemeAsync(_currentTheme);
    }

    private async Task LoadSavedThemeAsync()
    {
        try
        {
            var tenantId = Guid.Empty;
            var themeId = await _configRepository.GetValueAsync(tenantId, "Theme.Id", "y2k_retro");
            
            if (_availableThemes.TryGetValue(themeId, out var baseTheme))
            {
                _currentTheme = new ThemeSettings(
                    themeId,
                    baseTheme.ThemeName,
                    await _configRepository.GetValueAsync(tenantId, "Theme.BrandName", baseTheme.BrandName),
                    await _configRepository.GetValueAsync(tenantId, "Theme.PrimaryColor", baseTheme.PrimaryColor),
                    await _configRepository.GetValueAsync(tenantId, "Theme.SecondaryColor", baseTheme.SecondaryColor),
                    await _configRepository.GetValueAsync(tenantId, "Theme.AccentColor", baseTheme.AccentColor),
                    await _configRepository.GetValueAsync(tenantId, "Theme.FontFamily", baseTheme.FontFamily)
                );
                
                var bgImage = await _configRepository.GetValueAsync(tenantId, "Theme.BackgroundImage", "");
                if (!string.IsNullOrEmpty(bgImage))
                    _currentTheme.UpdateBackground(bgImage);
                
                var logoPath = await _configRepository.GetValueAsync(tenantId, "Theme.LogoPath", "");
                if (!string.IsNullOrEmpty(logoPath))
                    _currentTheme.UpdateBranding(_currentTheme.BrandName, logoPath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load saved theme, using default");
        }
    }

    private SolidColorBrush CreateSolidColorBrush(string hexColor)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.White);
        }
    }
}
