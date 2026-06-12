using System.Collections.ObjectModel;

namespace CorraStudio.Presentation.Wpf.Navigation;

public interface INavigationService
{
    event Action<string>? Navigated;
    event Action<Exception>? NavigationFailed;
    
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
    void NavigateTo(string viewModelName, object? parameter = null);
    void GoBack();
    void GoForward();
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    ViewModelBase? CurrentViewModel { get; }
    ObservableCollection<NavigationHistoryItem> BackStack { get; }
    ObservableCollection<NavigationHistoryItem> ForwardStack { get; }
    void ClearHistory();
    void NavigateToPrevious();
    void NavigateToNext();
    void SetParameter(object? parameter);
    object? CurrentParameter { get; }
}

public class NavigationHistoryItem
{
    public string ViewModelName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public object? Parameter { get; set; }
    public DateTime NavigatedAt { get; set; } = DateTime.Now;
}
