namespace CorraStudio.Domain.ValueObjects;

public class ThemeSettings : ValueObject
{
    public string ThemeId { get; private set; }
    public string ThemeName { get; private set; }
    public string BrandName { get; private set; }
    public string? LogoPath { get; private set; }
    public string? BackgroundImage { get; private set; }
    public string PrimaryColor { get; private set; }
    public string SecondaryColor { get; private set; }
    public string AccentColor { get; private set; }
    public string FontFamily { get; private set; }
    public bool EnableAnimations { get; private set; }
    public bool EnableSoundEffects { get; private set; }
    public Dictionary<string, string> CustomCss { get; private set; }

    public ThemeSettings(
        string themeId,
        string themeName,
        string brandName,
        string primaryColor,
        string secondaryColor,
        string accentColor,
        string fontFamily,
        bool enableAnimations = true,
        bool enableSoundEffects = false)
    {
        ThemeId = themeId;
        ThemeName = themeName;
        BrandName = brandName;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        AccentColor = accentColor;
        FontFamily = fontFamily;
        EnableAnimations = enableAnimations;
        EnableSoundEffects = enableSoundEffects;
        CustomCss = new Dictionary<string, string>();
    }

    public void UpdateBranding(string brandName, string? logoPath = null)
    {
        BrandName = brandName;
        if (logoPath != null)
            LogoPath = logoPath;
    }

    public void UpdateColors(string primary, string secondary, string accent)
    {
        PrimaryColor = primary;
        SecondaryColor = secondary;
        AccentColor = accent;
    }

    public void UpdateBackground(string? imagePath = null)
    {
        BackgroundImage = imagePath;
    }

    public void SetCustomCss(string key, string value)
    {
        CustomCss[key] = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ThemeId;
        yield return ThemeName;
    }

    // Pre-defined themes
    public static ThemeSettings Y2KRetro() => new ThemeSettings(
        "y2k_retro",
        "Y2K Retro",
        "Corra Studio",
        "#FFB7C5",  // Pink
        "#DDA0DD",  // Lavender
        "#FFCBA4",  // Peach
        "Comic Sans MS, Chalkboard SE, Marker Felt",
        true, true);

    public static ThemeSettings ModernMinimal() => new ThemeSettings(
        "modern_minimal",
        "Modern Minimal",
        "Corra Studio",
        "#FFFFFF",  // White
        "#F5F5F5",  // Light Gray
        "#2C3E50",  // Dark Blue
        "Segoe UI, Roboto, Helvetica Neue",
        true, false);

    public static ThemeSettings DarkMode() => new ThemeSettings(
        "dark_mode",
        "Dark Mode",
        "Corra Studio",
        "#1A1A2E",  // Dark Navy
        "#16213E",  // Darker Navy
        "#E94560",  // Neon Pink
        "Roboto, Segoe UI",
        true, false);

    public static ThemeSettings CoralPink() => new ThemeSettings(
        "coral_pink",
        "Coral Pink",
        "Corra Studio",
        "#FF6B6B",  // Coral
        "#FF8E8E",  // Light Coral
        "#4ECDC4",  // Mint
        "Poppins, Segoe UI",
        true, false);

    public static ThemeSettings Vintage() => new ThemeSettings(
        "vintage",
        "Vintage Sepia",
        "Corra Studio",
        "#F4E4C1",  // Cream
        "#D4A574",  // Sepia
        "#8B4513",  // Brown
        "Times New Roman, Georgia",
        true, false);

    public static ThemeSettings Wedding() => new ThemeSettings(
        "wedding",
        "Wedding Elegant",
        "Corra Studio",
        "#FFF5F5",  // Soft White
        "#F7D1D1",  // Soft Pink
        "#C9A96E",  // Gold
        "Playfair Display, Georgia",
        true, false);
}
