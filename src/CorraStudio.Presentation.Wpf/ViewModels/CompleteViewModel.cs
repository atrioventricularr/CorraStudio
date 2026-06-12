using System.Windows.Input;
using System.Windows.Media.Imaging;
using CorraStudio.Presentation.Wpf.Navigation;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class CompleteViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private BitmapImage? _qrCode;
    private string _downloadLink = string.Empty;
    private string _message = "Thank you for using Corra Studio!";
    private bool _showDownloadOptions = true;

    public CompleteViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        NewSessionCommand = new RelayCommand(() => _navigationService.NavigateTo<WelcomeViewModel>());
        PrintAgainCommand = new RelayCommand(() => OnPrintAgain());
        DownloadCommand = new RelayCommand(() => OnDownload());
        ExitCommand = new RelayCommand(() => System.Windows.Application.Current.Shutdown());
    }

    public BitmapImage? QrCode
    {
        get => _qrCode;
        set => SetField(ref _qrCode, value);
    }

    public string DownloadLink
    {
        get => _downloadLink;
        set => SetField(ref _downloadLink, value);
    }

    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }

    public bool ShowDownloadOptions
    {
        get => _showDownloadOptions;
        set => SetField(ref _showDownloadOptions, value);
    }

    public ICommand NewSessionCommand { get; }
    public ICommand PrintAgainCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand ExitCommand { get; }

    private void OnPrintAgain()
    {
        // Print again logic
    }

    private void OnDownload()
    {
        // Download logic
    }

    public override void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        // Generate QR code for download
        ShowDownloadOptions = true;
        Message = "Session completed successfully!";
    }
}
