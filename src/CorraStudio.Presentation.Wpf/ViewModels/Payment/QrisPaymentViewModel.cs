using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CorraStudio.Presentation.Wpf.ViewModels.Payment;

public class QrisPaymentViewModel : ViewModelBase
{
    private BitmapImage? _qrCode;
    private string _amountText = string.Empty;
    private string _merchantName = "Corra Studio";
    private string _statusMessage = "Waiting for payment...";
    private bool _isWaitingForConfirmation;
    private bool _isOperatorMode;

    public QrisPaymentViewModel()
    {
        ConfirmCommand = new RelayCommand(() => OnConfirmRequested?.Invoke());
        CancelCommand = new RelayCommand(() => OnCancelRequested?.Invoke());
        RefreshCommand = new RelayCommand(async () => await RefreshStatusAsync());
        
        // Start payment monitoring
        Task.Run(MonitorPaymentAsync);
    }

    public BitmapImage? QrCode
    {
        get => _qrCode;
        set => SetField(ref _qrCode, value);
    }

    public string AmountText
    {
        get => _amountText;
        set => SetField(ref _amountText, value);
    }

    public string MerchantName
    {
        get => _merchantName;
        set => SetField(ref _merchantName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public bool IsWaitingForConfirmation
    {
        get => _isWaitingForConfirmation;
        set => SetField(ref _isWaitingForConfirmation, value);
    }

    public bool IsOperatorMode
    {
        get => _isOperatorMode;
        set => SetField(ref _isOperatorMode, value);
    }

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RefreshCommand { get; }

    public event Action? OnConfirmRequested;
    public event Action? OnCancelRequested;

    public void SetPaymentDetails(string qrCodeData, decimal amount, string merchantName)
    {
        AmountText = $"Rp {amount:N0}";
        MerchantName = merchantName;
        
        // Generate QR code image from data
        GenerateQrImage(qrCodeData);
    }

    private void GenerateQrImage(string qrData)
    {
        // In production, generate actual QR code
        // For now, create placeholder
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri("pack://application:,,,/Resources/qr-placeholder.png", UriKind.RelativeOrAbsolute);
        bitmap.EndInit();
        QrCode = bitmap;
    }

    private async Task MonitorPaymentAsync()
    {
        await Task.Delay(1000);
        
        // Simulate waiting for payment
        IsWaitingForConfirmation = true;
        StatusMessage = "Please complete payment using your banking app";
        
        // In production, would poll payment status API
        for (int i = 0; i < 60; i++)
        {
            await Task.Delay(1000);
            // Check payment status
        }
        
        IsWaitingForConfirmation = false;
    }

    private async Task RefreshStatusAsync()
    {
        IsLoading = true;
        StatusMessage = "Checking payment status...";
        
        await Task.Delay(500);
        
        StatusMessage = IsWaitingForConfirmation 
            ? "Waiting for payment confirmation" 
            : "Payment status updated";
        
        IsLoading = false;
    }

    public void PaymentConfirmed()
    {
        StatusMessage = "Payment confirmed! Processing your order...";
        IsWaitingForConfirmation = false;
    }

    public void PaymentFailed(string error)
    {
        StatusMessage = $"Payment failed: {error}";
        IsWaitingForConfirmation = false;
    }
}
