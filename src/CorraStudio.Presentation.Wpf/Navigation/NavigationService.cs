using System.Collections.ObjectModel;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace CorraStudio.Presentation.Wpf.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<NavigationHistoryItem> _backStack = new();
    private readonly Stack<NavigationHistoryItem> _forwardStack = new();
    private Frame? _mainFrame;
    private ViewModelBase? _currentViewModel;
    private object? _currentParameter;

    public event Action<string>? Navigated;
    public event Action<Exception>? NavigationFailed;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(Frame mainFrame)
    {
        _mainFrame = mainFrame;
    }

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
    {
        NavigateTo(typeof(TViewModel).Name, parameter);
    }

    public void NavigateTo(string viewModelName, object? parameter = null)
    {
        try
        {
            if (_currentViewModel != null)
            {
                var historyItem = new NavigationHistoryItem
                {
                    ViewModelName = _currentViewModel.GetType().Name,
                    DisplayName = GetDisplayName(_currentViewModel),
                    Parameter = _currentParameter
                };
                _backStack.Push(historyItem);
                OnPropertyChanged(nameof(BackStack));
            }
            
            _forwardStack.Clear();
            OnPropertyChanged(nameof(ForwardStack));
            
            var viewModelType = Type.GetType($"CorraStudio.Presentation.Wpf.ViewModels.{viewModelName}");
            if (viewModelType == null)
                throw new Exception($"ViewModel {viewModelName} not found");
            
            var viewModel = _serviceProvider.GetRequiredService(viewModelType) as ViewModelBase;
            if (viewModel == null)
                throw new Exception($"Cannot create ViewModel {viewModelName}");
            
            viewModel.OnNavigatedTo(parameter);
            _currentViewModel = viewModel;
            _currentParameter = parameter;
            
            var view = CreateView(viewModelName);
            if (view != null && _mainFrame != null)
            {
                view.DataContext = viewModel;
                _mainFrame.Navigate(view);
            }
            
            Navigated?.Invoke(viewModelName);
        }
        catch (Exception ex)
        {
            NavigationFailed?.Invoke(ex);
            throw;
        }
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            var historyItem = _backStack.Pop();
            _forwardStack.Push(new NavigationHistoryItem
            {
                ViewModelName = _currentViewModel?.GetType().Name ?? string.Empty,
                DisplayName = GetDisplayName(_currentViewModel),
                Parameter = _currentParameter
            });
            
            NavigateTo(historyItem.ViewModelName, historyItem.Parameter);
            OnPropertyChanged(nameof(BackStack));
            OnPropertyChanged(nameof(ForwardStack));
        }
    }

    public void GoForward()
    {
        if (CanGoForward)
        {
            var historyItem = _forwardStack.Pop();
            _backStack.Push(new NavigationHistoryItem
            {
                ViewModelName = _currentViewModel?.GetType().Name ?? string.Empty,
                DisplayName = GetDisplayName(_currentViewModel),
                Parameter = _currentParameter
            });
            
            NavigateTo(historyItem.ViewModelName, historyItem.Parameter);
            OnPropertyChanged(nameof(BackStack));
            OnPropertyChanged(nameof(ForwardStack));
        }
    }

    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;
    public ViewModelBase? CurrentViewModel => _currentViewModel;
    public object? CurrentParameter => _currentParameter;

    public ObservableCollection<NavigationHistoryItem> BackStack 
    { 
        get => new ObservableCollection<NavigationHistoryItem>(_backStack.Reverse()); 
    }
    
    public ObservableCollection<NavigationHistoryItem> ForwardStack 
    { 
        get => new ObservableCollection<NavigationHistoryItem>(_forwardStack); 
    }

    public void ClearHistory()
    {
        _backStack.Clear();
        _forwardStack.Clear();
        OnPropertyChanged(nameof(BackStack));
        OnPropertyChanged(nameof(ForwardStack));
    }

    public void NavigateToPrevious()
    {
        if (CanGoBack)
            GoBack();
    }

    public void NavigateToNext()
    {
        if (CanGoForward)
            GoForward();
    }

    public void SetParameter(object? parameter)
    {
        _currentParameter = parameter;
    }

    private Page? CreateView(string viewModelName)
    {
        var viewName = viewModelName.Replace("ViewModel", "Page");
        var viewType = Type.GetType($"CorraStudio.Presentation.Wpf.Views.{viewName}");
        
        if (viewType != null)
            return Activator.CreateInstance(viewType) as Page;
        
        return null;
    }

    private string GetDisplayName(ViewModelBase? viewModel)
    {
        return viewModel switch
        {
            WelcomeViewModel => "Welcome",
            CaptureViewModel => "Capture",
            ReviewViewModel => "Review",
            PaymentViewModel => "Payment",
            ProcessingViewModel => "Processing",
            CompleteViewModel => "Complete",
            AdminViewModel => "Admin",
            _ => "Unknown"
        };
    }

    private void OnPropertyChanged(string propertyName)
    {
        // For UI binding updates
    }
}
