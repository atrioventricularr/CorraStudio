using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using CorraStudio.Domain.ValueObjects;
using CorraStudio.Presentation.Wpf.Services;

namespace CorraStudio.Presentation.Wpf.ViewModels.Admin;

public class ThemeSettingsViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private ObservableCollection<ThemeSettings> _availableThemes = new();
    private ThemeSettings? _selectedTheme;
    private string _brandName = string.Empty;
    private string _primaryColor = "#FFB7C5";
    private string _secondaryColor = "#DDA0DD";
    private string _accentColor = "#FFCBA4";
    private string _fontFamily = "Comic Sans MS";
    private string? _logoPath;
    private string? _backgroundPath;
    private bool _enableAnimations = true;
    private bool _enableSoundEffects;
    private bool _isSaving;
    private bool _isPreviewing;

    public ThemeSettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        
        LoadThemesCommand = new RelayCommand(async () => await LoadThemesAsync());
        ApplyThemeCommand = new RelayCommand<ThemeSettings>(async (t) => await ApplyThemeAsync(t));
        SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());
        ResetCommand = new RelayCommand(async () => await ResetAsync());
        SelectLogoCommand = new RelayCommand(async () => await SelectLogoAsync());
        SelectBackgroundCommand = new RelayCommand(async () => await SelectBackgroundAsync());
        PreviewCommand = new RelayCommand(async () => await PreviewAsync(), () => !IsPreviewing);
        
        Task.Run(async () => await LoadThemesAsync());
    }

    public ObservableCollection<ThemeSettings> AvailableThemes
    {
        get => _availableThemes;
        set => SetField(ref _availableThemes, value);
    }

    public ThemeSettings? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            SetField(ref _selectedTheme, value);
            if (value != null)
            {
                BrandName = value.BrandName;
                PrimaryColor = value.PrimaryColor;
                SecondaryColor = value.SecondaryColor;
                AccentColor = value.AccentColor;
                FontFamily = value.FontFamily;
            }
        }
    }

    public string BrandName
    {
        get => _brandName;
        set => SetField(ref _brandName, value);
    }

    public string PrimaryColor
    {
        get => _primaryColor;
        set => SetField(ref _primaryColor, value);
    }

    public string SecondaryColor
    {
        get => _secondaryColor;
        set => SetField(ref _secondaryColor, value);
    }

    public string AccentColor
    {
        get => _accentColor;
        set => SetField(ref _accentColor, value);
    }

    public string FontFamily
    {
        get => _fontFamily;
        set => SetField(ref _fontFamily, value);
    }

    public string? LogoPath
    {
        get => _logoPath;
        set => SetField(ref _logoPath, value);
    }

    public string? BackgroundPath
    {
        get => _backgroundPath;
        set => SetField(ref _backgroundPath, value);
    }

    public bool EnableAnimations
    {
        get => _enableAnimations;
        set => SetField(ref _enableAnimations, value);
    }

    public bool EnableSoundEffects
    {
        get => _enableSoundEffects;
        set => SetField(ref _enableSoundEffects, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetField(ref _isSaving, value);
    }

    public bool IsPreviewing
    {
        get => _isPreviewing;
        set => SetField(ref _isPreviewing, value);
    }

    public ICommand LoadThemesCommand { get; }
    public ICommand ApplyThemeCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand SelectLogoCommand { get; }
    public ICommand SelectBackgroundCommand { get; }
    public ICommand PreviewCommand { get; }

    private async Task LoadThemesAsync()
    {
        try
        {
            IsLoading = true;
            var themes = await _themeService.GetAvailableThemesAsync();
            AvailableThemes = new ObservableCollection<ThemeSettings>(themes);
            
            var currentTheme = await _themeService.GetCurrentThemeAsync();
            SelectedTheme = AvailableThemes.FirstOrDefault(t => t.ThemeId == currentTheme.ThemeId);
        }
        catch (Exception ex)
        {
            SetError($"Failed to load themes: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ApplyThemeAsync(ThemeSettings? theme)
    {
        if (theme == null) return;
        
        try
        {
            await _themeService.ApplyThemeAsync(theme);
            StatusMessage = $"Theme '{theme.ThemeName}' applied";
        }
        catch (Exception ex)
        {
            SetError($"Failed to apply theme: {ex.Message}");
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            IsSaving = true;
            
            var theme = new ThemeSettings(
                SelectedTheme?.ThemeId ?? "custom",
                SelectedTheme?.ThemeName ?? "Custom Theme",
                BrandName,
                PrimaryColor,
                SecondaryColor,
                AccentColor,
                FontFamily,
                EnableAnimations,
                EnableSoundEffects);
            
            if (!string.IsNullOrEmpty(LogoPath))
                theme.UpdateBranding(BrandName, LogoPath);
            
            if (!string.IsNullOrEmpty(BackgroundPath))
                theme.UpdateBackground(BackgroundPath);
            
            await _themeService.SaveThemeAsync(theme);
            await _themeService.ApplyThemeAsync(theme);
            
            StatusMessage = "Theme settings saved successfully";
        }
        catch (Exception ex)
        {
            SetError($"Failed to save: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task ResetAsync()
    {
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Reset all theme settings to default?",
            "Confirm Reset");
        
        if (confirmed)
        {
            await _themeService.ResetToDefaultAsync();
            await LoadThemesAsync();
            StatusMessage = "Theme reset to default";
        }
    }

    private async Task SelectLogoAsync()
    {
        var filePath = await DialogService.ShowFileOpenDialogAsync(
            "Image files|*.png;*.jpg;*.jpeg;*.bmp");
        
        if (!string.IsNullOrEmpty(filePath))
        {
            LogoPath = filePath;
            StatusMessage = "Logo selected";
        }
    }

    private async Task SelectBackgroundAsync()
    {
        var filePath = await DialogService.ShowFileOpenDialogAsync(
            "Image files|*.png;*.jpg;*.jpeg;*.bmp");
        
        if (!string.IsNullOrEmpty(filePath))
        {
            BackgroundPath = filePath;
            StatusMessage = "Background image selected";
        }
    }

    private async Task PreviewAsync()
    {
        IsPreviewing = true;
        
        var previewTheme = new ThemeSettings(
            "preview",
            "Preview",
            BrandName,
            PrimaryColor,
            SecondaryColor,
            AccentColor,
            FontFamily,
            EnableAnimations,
            EnableSoundEffects);
        
        if (!string.IsNullOrEmpty(BackgroundPath))
            previewTheme.UpdateBackground(BackgroundPath);
        
        await _themeService.ApplyThemeAsync(previewTheme);
        
        await Task.Delay(3000);
        
        // Revert after preview (or keep if user saves)
        IsPreviewing = false;
    }
}
