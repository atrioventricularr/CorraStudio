using System.Windows.Input;
using CorraStudio.Licensing.Services;

namespace CorraStudio.Presentation.Wpf.ViewModels.License;

public class LicenseStatusViewModel : ViewModelBase
{
    private readonly ILicenseValidationService _licenseService;
    private string _licenseStatus = "Checking...";
    private string _plan = string.Empty;
    private string _expiryDate = string.Empty;
    private string _licenseCode = string.Empty;
    private bool _isLicenseValid;

    public LicenseStatusViewModel(ILicenseValidationService licenseService)
    {
        _licenseService = licenseService;
        
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        ManageLicenseCommand = new RelayCommand(() => OnManageLicenseRequested?.Invoke());
        
        _licenseService.LicenseStatusChanged += OnLicenseStatusChanged;
        
        Task.Run(async () => await RefreshAsync());
    }

    public string LicenseStatus
    {
        get => _licenseStatus;
        set => SetField(ref _licenseStatus, value);
    }

    public string Plan
    {
        get => _plan;
        set => SetField(ref _plan, value);
    }

    public string ExpiryDate
    {
        get => _expiryDate;
        set => SetField(ref _expiryDate, value);
    }

    public string LicenseCode
    {
        get => _licenseCode;
        set => SetField(ref _licenseCode, value);
    }

    public bool IsLicenseValid
    {
        get => _isLicenseValid;
        set => SetField(ref _isLicenseValid, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ManageLicenseCommand { get; }

    public event Action? OnManageLicenseRequested;

    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            
            IsLicenseValid = await _licenseService.ValidateCurrentLicenseAsync();
            var license = _licenseService.GetCurrentLicense();
            
            if (license != null && IsLicenseValid)
            {
                LicenseStatus = "✅ Active";
                Plan = license.Plan;
                ExpiryDate = license.ExpiresAt.ToString("dd MMM yyyy");
                LicenseCode = license.LicenseCode;
            }
            else
            {
                LicenseStatus = "❌ Not Activated";
                Plan = "Trial";
                ExpiryDate = "N/A";
                LicenseCode = string.Empty;
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to check license: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnLicenseStatusChanged(object? sender, LicenseStatus status)
    {
        App.Current?.Dispatcher.Invoke(async () => await RefreshAsync());
    }
}
