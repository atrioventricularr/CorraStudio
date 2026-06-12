using CorraStudio.Presentation.Wpf.ViewModels.License;

namespace CorraStudio.Presentation.Wpf.Views.License;

public partial class LicenseActivationWindow : Window
{
    private readonly LicenseActivationViewModel _viewModel;

    public LicenseActivationWindow(LicenseActivationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        
        _viewModel.OnCloseRequested += () => Close();
        
        Loaded += async (s, e) => await _viewModel.CheckLicenseCommand.Execute(null);
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }
}
