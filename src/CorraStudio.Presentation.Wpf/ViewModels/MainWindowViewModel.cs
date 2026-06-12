using System.Windows.Input;
using CorraStudio.Presentation.Wpf.Services;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private string _currentViewName = "WelcomePage";
    private string _statusMessage = "Ready";
    private bool _isBusy;

    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        NavigateCommand = new RelayCommand<string>(Navigate);
        GoBackCommand = new RelayCommand(() => _navigationService.GoBack(), () => _navigationService.CanGoBack);
        GoForwardCommand = new RelayCommand(() => _navigationService.GoForward(), () => _navigationService.CanGoForward);
        ExitCommand = new RelayCommand(() => System.Windows.Application.Current.Shutdown());
    }

    public string CurrentViewName
    {
        get => _currentViewName;
        set => SetField(ref _currentViewName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public ICommand NavigateCommand { get; }
    public ICommand GoBackCommand { get; }
    public ICommand GoForwardCommand { get; }
    public ICommand ExitCommand { get; }

    private void Navigate(string viewName)
    {
        CurrentViewName = viewName;
        _navigationService.NavigateTo(viewName);
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

    public void Execute(object? parameter) => _execute(parameter);
}
