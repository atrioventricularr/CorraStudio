using System.Windows.Input;
using System.Windows.Media.Imaging;
using CorraStudio.Presentation.Wpf.Navigation;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class CaptureViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private BitmapImage? _livePreview;
    private int _countdownValue;
    private bool _isCountdownActive;
    private bool _isCapturing;
    private string _instruction = "Look at the camera and smile!";
    private int _photoCount;
    private int _maxPhotos = 4;

    public CaptureViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        CaptureCommand = new RelayCommand(() => StartCapture(), () => !IsCapturing && !IsCountdownActive);
        RetakeCommand = new RelayCommand(() => ResetCapture());
        ContinueCommand = new RelayCommand(() => _navigationService.NavigateTo<ReviewViewModel>(), () => PhotoCount > 0);
        CancelCommand = new RelayCommand(() => _navigationService.NavigateTo<WelcomeViewModel>());
    }

    public BitmapImage? LivePreview
    {
        get => _livePreview;
        set => SetField(ref _livePreview, value);
    }

    public int CountdownValue
    {
        get => _countdownValue;
        set => SetField(ref _countdownValue, value);
    }

    public bool IsCountdownActive
    {
        get => _isCountdownActive;
        set => SetField(ref _isCountdownActive, value);
    }

    public bool IsCapturing
    {
        get => _isCapturing;
        set => SetField(ref _isCapturing, value);
    }

    public string Instruction
    {
        get => _instruction;
        set => SetField(ref _instruction, value);
    }

    public int PhotoCount
    {
        get => _photoCount;
        set 
        { 
            SetField(ref _photoCount, value);
            ((RelayCommand)ContinueCommand).RaiseCanExecuteChanged();
        }
    }

    public int MaxPhotos
    {
        get => _maxPhotos;
        set => SetField(ref _maxPhotos, value);
    }

    public ICommand CaptureCommand { get; }
    public ICommand RetakeCommand { get; }
    public ICommand ContinueCommand { get; }
    public ICommand CancelCommand { get; }

    private async void StartCapture()
    {
        IsCountdownActive = true;
        for (int i = 3; i > 0; i--)
        {
            CountdownValue = i;
            await Task.Delay(1000);
        }
        IsCountdownActive = false;
        IsCapturing = true;
        
        // Simulate capture
        await Task.Delay(500);
        PhotoCount++;
        IsCapturing = false;
        
        if (PhotoCount >= MaxPhotos)
        {
            _navigationService.NavigateTo<ReviewViewModel>();
        }
    }

    private void ResetCapture()
    {
        PhotoCount = 0;
        Instruction = "Look at the camera and smile!";
    }

    public override void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        ResetCapture();
    }
}
