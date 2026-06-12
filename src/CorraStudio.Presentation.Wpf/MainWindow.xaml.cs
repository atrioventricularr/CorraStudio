using System.Windows;
using CorraStudio.Presentation.Wpf.Navigation;

namespace CorraStudio.Presentation.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel)
        {
            var navigationService = App.ServiceProvider?.GetService(typeof(INavigationService)) as INavigationService;
            
            if (MainFrame != null && navigationService is NavigationService navService)
            {
                navService.Initialize(MainFrame);
            }
        }
    }
}
