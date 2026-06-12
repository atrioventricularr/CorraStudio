using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CorraStudio.Payments.Models;

namespace CorraStudio.Presentation.Wpf.ViewModels.Admin;

public class QrisAdminViewModel : ViewModelBase
{
    private string _qrCodeData = string.Empty;
    private string _fixedAmount = "50000";
    private string _merchantName = "Corra Studio";
    private string _merchantCity = "Jakarta";
    private string _merchantId = "123456789012";
    private string _terminalId = "TERM001";
    private bool _isActive = true;
    private bool _allowCustomAmount;
    private string _minAmount = "10000";
    private string _maxAmount = "100000";
    private ObservableCollection<string> _availableAmounts = new();
    private string _newAmount = string.Empty;
    private BitmapImage? _qrPreview;
    private ObservableCollection<QrisTransaction> _recentTransactions = new();

    public QrisAdminViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        GeneratePreviewCommand = new RelayCommand(async () => await GeneratePreviewAsync());
        AddAmountCommand = new RelayCommand(AddAmount, () => !string.IsNullOrEmpty(NewAmount));
        RemoveAmountCommand = new RelayCommand<string>(RemoveAmount);
        RefreshTransactionsCommand = new RelayCommand(async () => await LoadTransactionsAsync());
        TestPaymentCommand = new RelayCommand(async () => await TestPaymentAsync());
        
        // Initialize sample available amounts
        AvailableAmounts.Add("50000");
        AvailableAmounts.Add("75000");
        AvailableAmounts.Add("100000");
        
        LoadData();
    }

    public string QrCodeData
    {
        get => _qrCodeData;
        set => SetField(ref _qrCodeData, value);
    }

    public string FixedAmount
    {
        get => _fixedAmount;
        set => SetField(ref _fixedAmount, value);
    }

    public string MerchantName
    {
        get => _merchantName;
        set => SetField(ref _merchantName, value);
    }

    public string MerchantCity
    {
        get => _merchantCity;
        set => SetField(ref _merchantCity, value);
    }

    public string MerchantId
    {
        get => _merchantId;
        set => SetField(ref _merchantId, value);
    }

    public string TerminalId
    {
        get => _terminalId;
        set => SetField(ref _terminalId, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    public bool AllowCustomAmount
    {
        get => _allowCustomAmount;
        set => SetField(ref _allowCustomAmount, value);
    }

    public string MinAmount
    {
        get => _minAmount;
        set => SetField(ref _minAmount, value);
    }

    public string MaxAmount
    {
        get => _maxAmount;
        set => SetField(ref _maxAmount, value);
    }

    public ObservableCollection<string> AvailableAmounts
    {
        get => _availableAmounts;
        set => SetField(ref _availableAmounts, value);
    }

    public string NewAmount
    {
        get => _newAmount;
        set => SetField(ref _newAmount, value);
    }

    public BitmapImage? QrPreview
    {
        get => _qrPreview;
        set => SetField(ref _qrPreview, value);
    }

    public ObservableCollection<QrisTransaction> RecentTransactions
    {
        get => _recentTransactions;
        set => SetField(ref _recentTransactions, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand GeneratePreviewCommand { get; }
    public ICommand AddAmountCommand { get; }
    public ICommand RemoveAmountCommand { get; }
    public ICommand RefreshTransactionsCommand { get; }
    public ICommand TestPaymentCommand { get; }

    private async void LoadData()
    {
        try
        {
            IsLoading = true;
            await LoadTransactionsAsync();
            await GeneratePreviewAsync();
            StatusMessage = "QRIS settings loaded";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load: {ex.Message}");
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
            StatusMessage = "Saving QRIS settings...";
            
            await Task.Delay(500);
            
            StatusMessage = "QRIS settings saved successfully";
            await DialogService.ShowMessageAsync("QRIS settings have been saved.", "Success");
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

    private async Task GeneratePreviewAsync()
    {
        try
        {
            await Task.Delay(100);
            
            // Simulate QR code generation
            using var stream = new MemoryStream();
            using var bitmap = new SkiaSharp.SKBitmap(200, 200);
            using var canvas = new SkiaSharp.SKCanvas(bitmap);
            
            canvas.Clear(SkiaSharp.SKColors.White);
            
            // Draw mock QR pattern
            using var paint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black };
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    if ((i + j) % 2 == 0)
                    {
                        var rect = new SkiaSharp.SKRect(i * 10, j * 10, (i + 1) * 10, (j + 1) * 10);
                        canvas.DrawRect(rect, paint);
                    }
                }
            }
            
            using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            var bytes = data.ToArray();
            
            using var ms = new MemoryStream(bytes);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            
            QrPreview = bitmapImage;
        }
        catch (Exception ex)
        {
            SetError($"Failed to generate preview: {ex.Message}");
        }
    }

    private void AddAmount()
    {
        if (!string.IsNullOrEmpty(NewAmount) && !AvailableAmounts.Contains(NewAmount))
        {
            AvailableAmounts.Add(NewAmount);
            NewAmount = string.Empty;
        }
    }

    private void RemoveAmount(string amount)
    {
        if (!string.IsNullOrEmpty(amount))
        {
            AvailableAmounts.Remove(amount);
        }
    }

    private async Task LoadTransactionsAsync()
    {
        await Task.Delay(200);
        
        // Mock data
        RecentTransactions.Clear();
        RecentTransactions.Add(new QrisTransaction
        {
            TransactionCode = "QRIS-001",
            Amount = 50000,
            Status = QrisTransactionStatus.Paid,
            CreatedAt = DateTime.Now.AddMinutes(-5),
            PaidAt = DateTime.Now.AddMinutes(-4)
        });
        RecentTransactions.Add(new QrisTransaction
        {
            TransactionCode = "QRIS-002",
            Amount = 75000,
            Status = QrisTransactionStatus.Pending,
            CreatedAt = DateTime.Now.AddMinutes(-10)
        });
    }

    private async Task TestPaymentAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Testing QRIS payment...";
            
            await Task.Delay(1500);
            
            await DialogService.ShowMessageAsync(
                "QRIS payment test successful!\n\n" +
                $"Merchant: {MerchantName}\n" +
                $"Amount: Rp {FixedAmount}\n" +
                "QR code has been generated.", 
                "Test Result");
            
            StatusMessage = "Test completed";
        }
        catch (Exception ex)
        {
            SetError($"Test failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
