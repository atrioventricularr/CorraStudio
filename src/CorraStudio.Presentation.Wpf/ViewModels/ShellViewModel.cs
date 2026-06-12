using System.Collections.ObjectModel;
using System.Windows.Input;
using CorraStudio.Presentation.Wpf.Navigation;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private bool _isNavigationBarVisible = true;
    private string _title = "Corra Studio";
    private string _currentPageTitle = string.Empty;
    private bool _isBusy;
    private string _statusMessage = "Ready";

    public ShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        GoBackCommand = new RelayCommand(() => _navigationService.GoBack(), () => _navigationService.CanGoBack);
        GoForwardCommand = new RelayCommand(() => _navigationService.GoForward(), () => _navigationService.CanGoForward);
        NavigateCommand = new RelayCommand<string>(Navigate);
        ToggleNavigationBarCommand = new RelayCommand(() => IsNavigationBarVisible = !IsNavigationBarVisible);
        ExitCommand = new RelayCommand(() => System.Windows.Application.Current.Shutdown());
        
        _navigationService.Navigated += OnNavigated;
        _navigationService.NavigationFailed += OnNavigationFailed;
    }

    public bool IsNavigationBarVisible
    {
        get => _isNavigationBarVisible;
        set => SetField(ref _isNavigationBarVisible, value);
    }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        set => SetField(ref _currentPageTitle, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ObservableCollection<NavigationHistoryItem> BackStack => _navigationService.BackStack;
    public ObservableCollection<NavigationHistoryItem> ForwardStack => _navigationService.ForwardStack;

    public ICommand GoBackCommand { get; }
    public ICommand GoForwardCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand ToggleNavigationBarCommand { get; }
    public ICommand ExitCommand { get; }

    private void Navigate(string viewModelName)
    {
        try
        {
            IsBusy = true;
            _navigationService.NavigateTo(viewModelName);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Navigation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnNavigated(string viewModelName)
    {
        CurrentPageTitle = GetPageTitle(viewModelName);
        StatusMessage = $"Navigated to {CurrentPageTitle}";
        
        ((RelayCommand)GoBackCommand).RaiseCanExecuteChanged();
        ((RelayCommand)GoForwardCommand).RaiseCanExecuteChanged();
        
        OnPropertyChanged(nameof(BackStack));
        OnPropertyChanged(nameof(ForwardStack));
    }

    private void OnNavigationFailed(Exception ex)
    {
        StatusMessage = $"Navigation failed: {ex.Message}";
        SetError(ex.Message);
    }

    private string GetPageTitle(string viewModelName)
    {
        return viewModelName switch
        {
            "WelcomeViewModel" => "Welcome",
            "CaptureViewModel" => "Photo Capture",
            "ReviewViewModel" => "Review Photos",
            "PaymentViewModel" => "Payment",
            "ProcessingViewModel" => "Processing",
            "CompleteViewModel" => "Complete",
            "AdminViewModel" => "Administration",
            _ => "Corra Studio"
        };
    }
}
