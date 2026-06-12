using System.Windows.Input;
using CorraStudio.Licensing.Services;

namespace CorraStudio.Presentation.Wpf.ViewModels.Admin;

public class LicenseSettingsViewModel : ViewModelBase
{
    private readonly ILicenseValidationService _licenseService;
    private string _licenseCode = string.Empty;
    private string _status = string.Empty;
    private string _plan = string.Empty;
    private string _expiryDate = string.Empty;
    private string _deviceId = string.Empty;
    private bool _isLicenseValid;

    public LicenseSettingsViewModel(ILicenseValidationService licenseService)
    {
        _licenseService = licenseService;
        
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        ValidateCommand = new RelayCommand(async () => await ValidateLicenseAsync());
        DeactivateCommand = new RelayCommand(async () => await DeactivateLicenseAsync(), () => IsLicenseValid);
        ReactivateCommand = new RelayCommand(() => OnReactivateRequested?.Invoke());
        
        Task.Run(async () => await RefreshAsync());
    }

    public string LicenseCode
    {
        get => _licenseCode;
        set => SetField(ref _licenseCode, value);
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
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

    public string DeviceId
    {
        get => _deviceId;
        set => SetField(ref _deviceId, value);
    }

    public bool IsLicenseValid
    {
        get => _isLicenseValid;
        set
        {
            SetField(ref _isLicenseValid, value);
            ((RelayCommand)DeactivateCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand DeactivateCommand { get; }
    public ICommand ReactivateCommand { get; }

    public event Action? OnReactivateRequested;

    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            
            IsLicenseValid = await _licenseService.ValidateCurrentLicenseAsync();
            var license = _licenseService.GetCurrentLicense();
            
            if (license != null && IsLicenseValid)
            {
                Status = "✅ Active";
                Plan = license.Plan;
                ExpiryDate = license.ExpiresAt.ToString("dd MMM yyyy");
                LicenseCode = license.LicenseCode;
                DeviceId = license.DeviceId?.Substring(0, Math.Min(20, license.DeviceId.Length ?? 0)) ?? "N/A";
            }
            else
            {
                Status = "❌ Not Activated";
                Plan = "Trial";
                ExpiryDate = "N/A";
                LicenseCode = "Not activated";
                DeviceId = "N/A";
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to load license status: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ValidateLicenseAsync()
    {
        try
        {
            IsLoading = true;
            var isValid = await _licenseService.ValidateCurrentLicenseAsync();
            
            if (isValid)
            {
                Status = "✅ License is valid";
                await RefreshAsync();
            }
            else
            {
                Status = "❌ License is invalid or expired";
            }
        }
        catch (Exception ex)
        {
            Status = $"Validation failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeactivateLicenseAsync()
    {
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Deactivate license on this device?\n\n" +
            "You will need to reactivate to use this software again.",
            "Confirm Deactivation");
        
        if (confirmed)
        {
            try
            {
                IsLoading = true;
                var success = await _licenseService.DeactivateLicenseAsync();
                
                if (success)
                {
                    Status = "License deactivated";
                    await RefreshAsync();
                }
                else
                {
                    Status = "Failed to deactivate license";
                }
            }
            catch (Exception ex)
            {
                Status = $"Deactivation failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
