using System.Windows;
using System.Windows.Controls;

namespace CorraStudio.Presentation.Wpf.Services;

public interface INavigationService
{
    void NavigateTo(string viewName);
    void GoBack();
    void GoForward();
    bool CanGoBack { get; }
    bool CanGoForward { get; }
}

public class NavigationService : INavigationService
{
    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();
    private string? _currentView;
    private Frame? _mainFrame;

    public void Initialize(Frame mainFrame)
    {
        _mainFrame = mainFrame;
    }

    public void NavigateTo(string viewName)
    {
        if (_currentView != null)
            _backStack.Push(_currentView);
        
        _forwardStack.Clear();
        _currentView = viewName;
        
        var viewType = Type.GetType($"CorraStudio.Presentation.Wpf.Views.{viewName}");
        if (viewType != null && _mainFrame != null)
        {
            var view = Activator.CreateInstance(viewType) as Page;
            _mainFrame.Navigate(view);
        }
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            _forwardStack.Push(_currentView!);
            _currentView = _backStack.Pop();
            NavigateTo(_currentView);
        }
    }

    public void GoForward()
    {
        if (CanGoForward)
        {
            _backStack.Push(_currentView!);
            _currentView = _forwardStack.Pop();
            NavigateTo(_currentView);
        }
    }

    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;
}
