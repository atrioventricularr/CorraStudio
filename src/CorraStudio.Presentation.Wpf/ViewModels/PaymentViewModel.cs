using System.Windows.Input;
using System.Windows.Media.Imaging;
using CorraStudio.Presentation.Wpf.Navigation;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class PaymentViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private decimal _totalAmount = 50000;
    private string _selectedMethod = "QRIS";
    private BitmapImage? _qrCode;
    private string _transactionCode = string.Empty;
    private bool _isProcessing;
    private string _statusMessage = "Select payment method";

    public PaymentViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        SelectMethodCommand = new RelayCommand<string>(SelectMethod);
        ProcessPaymentCommand = new RelayCommand(ProcessPayment, () => !IsProcessing);
        CancelCommand = new RelayCommand(() => _navigationService.GoBack());
    }

    public decimal TotalAmount
    {
        get => _totalAmount;
        set => SetField(ref _totalAmount, value);
    }

    public string SelectedMethod
    {
        get => _selectedMethod;
        set => SetField(ref _selectedMethod, value);
    }

    public BitmapImage? QrCode
    {
        get => _qrCode;
        set => SetField(ref _qrCode, value);
    }

    public string TransactionCode
    {
        get => _transactionCode;
        set => SetField(ref _transactionCode, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            SetField(ref _isProcessing, value);
            ((RelayCommand)ProcessPaymentCommand).RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ICommand SelectMethodCommand { get; }
    public ICommand ProcessPaymentCommand { get; }
    public ICommand CancelCommand { get; }

    private void SelectMethod(string method)
    {
        SelectedMethod = method;
        StatusMessage = $"Selected {method} payment method";
    }

    private async void ProcessPayment()
    {
        IsProcessing = true;
        StatusMessage = "Processing payment...";
        
        await Task.Delay(2000); // Simulate payment processing
        
        IsProcessing = false;
        _navigationService.NavigateTo<ProcessingViewModel>();
    }

    public override void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        TransactionCode = $"TRX-{DateTime.Now:yyyyMMddHHmmss}";
        
        if (SelectedMethod == "QRIS")
        {
            // Generate QR code
        }
    }
}
