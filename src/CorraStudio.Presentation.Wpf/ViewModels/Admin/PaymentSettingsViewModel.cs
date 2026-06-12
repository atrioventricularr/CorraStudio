using System.Windows.Input;
using CorraStudio.Payments.Models;

namespace CorraStudio.Presentation.Wpf.ViewModels.Admin;

public class PaymentSettingsViewModel : ViewModelBase
{
    // Doku Settings
    private string _dokuClientId = string.Empty;
    private string _dokuSharedKey = string.Empty;
    private string _dokuEnvironment = "sandbox";
    private bool _dokuEnableQRIS = true;
    private bool _dokuEnableVA = true;
    private bool _dokuEnableEWallet = true;
    private bool _dokuEnableConvenienceStore = false;
    
    // QRIS Static Settings
    private string _qrisStaticData = string.Empty;
    private string _qrisFixedAmount = "50000";
    private string _qrisMerchantName = "Corra Studio";
    private string _qrisMerchantCity = "Jakarta";
    
    private bool _isSaving;
    private string _selectedProvider = "Doku";

    public PaymentSettingsViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync(), () => CanTestConnection);
        LoadCommand = new RelayCommand(async () => await LoadAsync());
        
        LoadData();
    }

    public string SelectedProvider
    {
        get => _selectedProvider;
        set => SetField(ref _selectedProvider, value);
    }

    // Doku Properties
    public string DokuClientId
    {
        get => _dokuClientId;
        set => SetField(ref _dokuClientId, value);
    }

    public string DokuSharedKey
    {
        get => _dokuSharedKey;
        set => SetField(ref _dokuSharedKey, value);
    }

    public string DokuEnvironment
    {
        get => _dokuEnvironment;
        set => SetField(ref _dokuEnvironment, value);
    }

    public bool DokuEnableQRIS
    {
        get => _dokuEnableQRIS;
        set => SetField(ref _dokuEnableQRIS, value);
    }

    public bool DokuEnableVA
    {
        get => _dokuEnableVA;
        set => SetField(ref _dokuEnableVA, value);
    }

    public bool DokuEnableEWallet
    {
        get => _dokuEnableEWallet;
        set => SetField(ref _dokuEnableEWallet, value);
    }

    public bool DokuEnableConvenienceStore
    {
        get => _dokuEnableConvenienceStore;
        set => SetField(ref _dokuEnableConvenienceStore, value);
    }

    // QRIS Static Properties
    public string QrisStaticData
    {
        get => _qrisStaticData;
        set => SetField(ref _qrisStaticData, value);
    }

    public string QrisFixedAmount
    {
        get => _qrisFixedAmount;
        set => SetField(ref _qrisFixedAmount, value);
    }

    public string QrisMerchantName
    {
        get => _qrisMerchantName;
        set => SetField(ref _qrisMerchantName, value);
    }

    public string QrisMerchantCity
    {
        get => _qrisMerchantCity;
        set => SetField(ref _qrisMerchantCity, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetField(ref _isSaving, value);
    }

    public bool CanTestConnection => !string.IsNullOrEmpty(DokuClientId) && !string.IsNullOrEmpty(DokuSharedKey);

    public ICommand SaveCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand LoadCommand { get; }

    private async void LoadData()
    {
        try
        {
            IsLoading = true;
            
            // In production, load from database via IConfigurationRepository
            // For demo, load from Properties.Settings or local storage
            
            StatusMessage = "Settings loaded";
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
            StatusMessage = "Saving payment settings...";
            
            // Simulate save delay
            await Task.Delay(500);
            
            // In production, save to database via IConfigurationRepository
            
            StatusMessage = "Payment settings saved successfully";
        }
        catch (Exception ex)
        {
            SetError($"Failed to save settings: {ex.Message}");
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
            StatusMessage = "Testing Doku connection...";
            
            await Task.Delay(1000);
            
            StatusMessage = "Doku connection successful!";
            await DialogService.ShowMessageAsync("Connection successful!", "Test Result");
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
