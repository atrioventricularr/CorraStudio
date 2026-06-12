using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace CorraStudio.Presentation.Wpf.Behaviors;

public class NavigationBehavior : Behavior<Frame>
{
    public static readonly DependencyProperty NavigationServiceProperty =
        DependencyProperty.Register(nameof(NavigationService), typeof(INavigationService), typeof(NavigationBehavior), new PropertyMetadata(null));

    public INavigationService? NavigationService
    {
        get => (INavigationService?)GetValue(NavigationServiceProperty);
        set => SetValue(NavigationServiceProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.Navigating += OnNavigating;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnLoaded;
        AssociatedObject.Navigating -= OnNavigating;
        base.OnDetaching();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NavigationService?.Initialize(AssociatedObject);
    }

    private void OnNavigating(object sender, NavigatingCancelEventArgs e)
    {
        // Handle navigation events if needed
    }
}
