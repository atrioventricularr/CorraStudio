using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CorraStudio.Presentation.Wpf.ViewModels.Payment.Midtrans;

public class MidtransPaymentViewModel : ViewModelBase
{
    private string _amountText = string.Empty;
    private string _statusMessage = "Select payment method";
    private string _selectedMethod = string.Empty;
    private BitmapImage? _qrCode;
    private string _virtualAccountNumber = string.Empty;
    private string _paymentCode = string.Empty;
    private bool _showQrCode;
    private bool _showVirtualAccount;
    private bool _isProcessing;

    public MidtransPaymentViewModel()
    {
        SelectMethodCommand = new RelayCommand<string>(SelectMethod);
        ProcessPaymentCommand = new RelayCommand(async () => await ProcessPaymentAsync(), () => !IsProcessing && !string.IsNullOrEmpty(SelectedMethod));
        CancelCommand = new RelayCommand(() => OnCancelRequested?.Invoke());
        
        LoadData();
    }

    public string AmountText
    {
        get => _amountText;
        set => SetField(ref _amountText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
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

    public string VirtualAccountNumber
    {
        get => _virtualAccountNumber;
        set => SetField(ref _virtualAccountNumber, value);
    }

    public string PaymentCode
    {
        get => _paymentCode;
        set => SetField(ref _paymentCode, value);
    }

    public bool ShowQrCode
    {
        get => _showQrCode;
        set => SetField(ref _showQrCode, value);
    }

    public bool ShowVirtualAccount
    {
        get => _showVirtualAccount;
        set => SetField(ref _showVirtualAccount, value);
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

    public ICommand SelectMethodCommand { get; }
    public ICommand ProcessPaymentCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action<string>? PaymentMethodSelected;
    public event Action? PaymentProcessed;
    public event Action? OnCancelRequested;

    private void LoadData()
    {
        AmountText = "Rp 50,000";
    }

    private void SelectMethod(string method)
    {
        SelectedMethod = method;
        StatusMessage = $"Selected {GetMethodDisplayName(method)}";
        
        ShowQrCode = false;
        ShowVirtualAccount = false;
        
        if (method == "QRIS")
        {
            ShowQrCode = true;
            GenerateMockQrCode();
        }
        else if (method == "VirtualAccount")
        {
            ShowVirtualAccount = true;
            VirtualAccountNumber = GenerateVaNumber();
        }
        else if (method == "ConvenienceStore")
        {
            PaymentCode = GeneratePaymentCode();
        }
        
        PaymentMethodSelected?.Invoke(method);
    }

    private async Task ProcessPaymentAsync()
    {
        IsProcessing = true;
        StatusMessage = $"Processing {GetMethodDisplayName(SelectedMethod)} payment...";
        
        await Task.Delay(2000);
        
        IsProcessing = false;
        StatusMessage = "Payment successful! Redirecting...";
        PaymentProcessed?.Invoke();
    }

    private string GetMethodDisplayName(string method)
    {
        return method switch
        {
            "QRIS" => "QRIS",
            "CreditCard" => "Credit Card",
            "VirtualAccount" => "Virtual Account",
            "Ewallet" => "E-Wallet",
            "ConvenienceStore" => "Convenience Store",
            _ => method
        };
    }

    private void GenerateMockQrCode()
    {
        // In production, generate actual QR code from Midtrans
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri("pack://application:,,,/Resources/midtrans-qr-placeholder.png", UriKind.RelativeOrAbsolute);
        bitmap.EndInit();
        QrCode = bitmap;
    }

    private string GenerateVaNumber()
    {
        return $"8808{DateTime.Now:yyMMdd}{new Random().Next(1000, 9999)}";
    }

    private string GeneratePaymentCode()
    {
        return $"MID-{DateTime.Now:yyyyMMdd}-{new Random().Next(10000, 99999)}";
    }

    public void SetAmount(decimal amount)
    {
        AmountText = $"Rp {amount:N0}";
    }

    public void ShowPaymentSuccess()
    {
        StatusMessage = "Payment completed successfully!";
    }

    public void ShowPaymentError(string error)
    {
        StatusMessage = $"Payment failed: {error}";
    }
}
