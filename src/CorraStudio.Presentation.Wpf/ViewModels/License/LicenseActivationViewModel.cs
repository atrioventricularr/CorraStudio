using System.Windows.Input;
using CorraStudio.Licensing.Services;

namespace CorraStudio.Presentation.Wpf.ViewModels.License;

public class LicenseActivationViewModel : ViewModelBase
{
    private readonly ILicenseValidationService _licenseService;
    private string _licenseCode = string.Empty;
    private string _email = string.Empty;
    private bool _isActivating;
    private string _statusMessage = "Enter your license code to activate";
    private bool _hasError;

    public LicenseActivationViewModel(ILicenseValidationService licenseService)
    {
        _licenseService = licenseService;
        
        ActivateCommand = new RelayCommand(async () => await ActivateAsync(), () => CanActivate);
        CloseCommand = new RelayCommand(() => OnCloseRequested?.Invoke());
        CheckLicenseCommand = new RelayCommand(async () => await CheckLicenseAsync());
    }

    public string LicenseCode
    {
        get => _licenseCode;
        set
        {
            SetField(ref _licenseCode, value);
            ((RelayCommand)ActivateCommand).RaiseCanExecuteChanged();
            HasError = false;
            StatusMessage = "Enter your license code to activate";
        }
    }

    public string Email
    {
        get => _email;
        set => SetField(ref _email, value);
    }

    public bool IsActivating
    {
        get => _isActivating;
        set => SetField(ref _isActivating, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetField(ref _hasError, value);
    }

    public bool CanActivate => !string.IsNullOrEmpty(LicenseCode) && LicenseCode.Length >= 16;

    public ICommand ActivateCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand CheckLicenseCommand { get; }

    public event Action? OnCloseRequested;
    public event Action<bool>? ActivationCompleted;

    private async Task ActivateAsync()
    {
        try
        {
            IsActivating = true;
            StatusMessage = "Validating license code...";
            HasError = false;
            
            var success = await _licenseService.ActivateLicenseAsync(LicenseCode, Email);
            
            if (success)
            {
                StatusMessage = "License activated successfully!";
                ActivationCompleted?.Invoke(true);
                await Task.Delay(1000);
                OnCloseRequested?.Invoke();
            }
            else
            {
                StatusMessage = "Invalid license code. Please check and try again.";
                HasError = true;
                ActivationCompleted?.Invoke(false);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Activation failed: {ex.Message}";
            HasError = true;
            ActivationCompleted?.Invoke(false);
        }
        finally
        {
            IsActivating = false;
        }
    }

    private async Task CheckLicenseAsync()
    {
        var isValid = await _licenseService.ValidateCurrentLicenseAsync();
        
        if (isValid)
        {
            StatusMessage = "License is valid!";
            ActivationCompleted?.Invoke(true);
            OnCloseRequested?.Invoke();
        }
        else
        {
            StatusMessage = "No valid license found. Please activate.";
            HasError = true;
        }
    }
}
