using System.Windows.Input;
using CorraStudio.Presentation.Wpf.Navigation;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class ProcessingViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private string _status = "Processing your photos...";
    private int _progressPercentage;
    private string _currentTask = "Preparing...";
    private bool _showProgress = true;

    public ProcessingViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        CancelCommand = new RelayCommand(() => _navigationService.NavigateTo<WelcomeViewModel>());
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public int ProgressPercentage
    {
        get => _progressPercentage;
        set => SetField(ref _progressPercentage, value);
    }

    public string CurrentTask
    {
        get => _currentTask;
        set => SetField(ref _currentTask, value);
    }

    public bool ShowProgress
    {
        get => _showProgress;
        set => SetField(ref _showProgress, value);
    }

    public ICommand CancelCommand { get; }

    public override async void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        
        // Simulate processing
        var steps = new[] { "Rendering photos...", "Generating QR code...", "Printing...", "Finalizing..." };
        
        for (int i = 0; i < steps.Length; i++)
        {
            CurrentTask = steps[i];
            ProgressPercentage = (i + 1) * 25;
            await Task.Delay(1000);
        }
        
        _navigationService.NavigateTo<CompleteViewModel>();
    }
}
