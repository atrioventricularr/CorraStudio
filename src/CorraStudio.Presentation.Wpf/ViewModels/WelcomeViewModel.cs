using System.Windows.Input;
using CorraStudio.Presentation.Wpf.Navigation;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class WelcomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private string _greeting = "Welcome to Corra Studio";
    private string _subtitle = "Professional Photobooth Software";
    private bool _isDemoMode;

    public WelcomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        StartCommand = new RelayCommand(() => _navigationService.NavigateTo<CaptureViewModel>());
        SettingsCommand = new RelayCommand(() => _navigationService.NavigateTo("SettingsViewModel"));
        AdminCommand = new RelayCommand(() => _navigationService.NavigateTo<AdminViewModel>());
    }

    public string Greeting
    {
        get => _greeting;
        set => SetField(ref _greeting, value);
    }

    public string Subtitle
    {
        get => _subtitle;
        set => SetField(ref _subtitle, value);
    }

    public bool IsDemoMode
    {
        get => _isDemoMode;
        set => SetField(ref _isDemoMode, value);
    }

    public ICommand StartCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand AdminCommand { get; }

    public override void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        // Check demo mode or any initialization
        IsDemoMode = false;
    }
}
