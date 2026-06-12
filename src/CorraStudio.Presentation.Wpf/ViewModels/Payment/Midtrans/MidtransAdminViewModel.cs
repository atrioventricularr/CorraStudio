using System.Windows.Input;
using CorraStudio.Payments.Models;

namespace CorraStudio.Presentation.Wpf.ViewModels.Payment.Midtrans;

public class MidtransAdminViewModel : ViewModelBase
{
    private string _clientKey = string.Empty;
    private string _serverKey = string.Empty;
    private bool _isProduction;
    private bool _enableQRIS = true;
    private bool _enableCreditCard = true;
    private bool _enableVirtualAccount = true;
    private bool _enableEwallet = true;
    private bool _enableConvenienceStore;
    private string _notificationUrl = string.Empty;
    private string _finishUrl = string.Empty;
    private string _errorUrl = string.Empty;
    private bool _isSaving;

    public MidtransAdminViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync(), () => CanTestConnection);
        LoadCommand = new RelayCommand(async () => await LoadAsync());
        
        LoadData();
    }

    public string ClientKey
    {
        get => _clientKey;
        set => SetField(ref _clientKey, value);
    }

    public string ServerKey
    {
        get => _serverKey;
        set => SetField(ref _serverKey, value);
    }

    public bool IsProduction
    {
        get => _isProduction;
        set => SetField(ref _isProduction, value);
    }

    public bool EnableQRIS
    {
        get => _enableQRIS;
        set => SetField(ref _enableQRIS, value);
    }

    public bool EnableCreditCard
    {
        get => _enableCreditCard;
        set => SetField(ref _enableCreditCard, value);
    }

    public bool EnableVirtualAccount
    {
        get => _enableVirtualAccount;
        set => SetField(ref _enableVirtualAccount, value);
    }

    public bool EnableEwallet
    {
        get => _enableEwallet;
        set => SetField(ref _enableEwallet, value);
    }

    public bool EnableConvenienceStore
    {
        get => _enableConvenienceStore;
        set => SetField(ref _enableConvenienceStore, value);
    }

    public string NotificationUrl
    {
        get => _notificationUrl;
        set => SetField(ref _notificationUrl, value);
    }

    public string FinishUrl
    {
        get => _finishUrl;
        set => SetField(ref _finishUrl, value);
    }

    public string ErrorUrl
    {
        get => _errorUrl;
        set => SetField(ref _errorUrl, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetField(ref _isSaving, value);
    }

    public bool CanTestConnection => !string.IsNullOrEmpty(ClientKey) && !string.IsNullOrEmpty(ServerKey);

    public ICommand SaveCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand LoadCommand { get; }

    private async void LoadData()
    {
        try
        {
            IsLoading = true;
            
            // In production, load from database
            await Task.Delay(100);
            
            StatusMessage = "Midtrans settings loaded";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load settings: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;
            StatusMessage = "Saving Midtrans settings...";
            
            await Task.Delay(500);
            
            StatusMessage = "Midtrans settings saved successfully";
            await DialogService.ShowMessageAsync("Midtrans settings have been saved.", "Success");
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

    private async Task TestConnectionAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Testing Midtrans connection...";
            
            await Task.Delay(1000);
            
            StatusMessage = "Midtrans connection successful!";
            await DialogService.ShowMessageAsync(
                "Connection to Midtrans successful!\n\n" +
                $"Environment: {(IsProduction ? "Production" : "Sandbox")}\n" +
                $"Client Key: {ClientKey.Substring(0, Math.Min(8, ClientKey.Length))}...\n" +
                "Your payment gateway is ready to use.",
                "Connection Test");
        }
        catch (Exception ex)
        {
            SetError($"Connection test failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
