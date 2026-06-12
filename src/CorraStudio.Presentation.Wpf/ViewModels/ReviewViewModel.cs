using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CorraStudio.Presentation.Wpf.Navigation;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class ReviewViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private ObservableCollection<PhotoItem> _photos = new();
    private PhotoItem? _selectedPhoto;
    private int _selectedCount;

    public ReviewViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        SelectCommand = new RelayCommand<PhotoItem>(SelectPhoto);
        RetakeCommand = new RelayCommand(() => _navigationService.GoBack());
        PrintCommand = new RelayCommand(() => OnPrint(), () => HasSelectedPhotos);
        ContinueToPaymentCommand = new RelayCommand(() => _navigationService.NavigateTo<PaymentViewModel>(), () => HasSelectedPhotos);
        CancelCommand = new RelayCommand(() => _navigationService.NavigateTo<WelcomeViewModel>());
    }

    public ObservableCollection<PhotoItem> Photos
    {
        get => _photos;
        set => SetField(ref _photos, value);
    }

    public PhotoItem? SelectedPhoto
    {
        get => _selectedPhoto;
        set => SetField(ref _selectedPhoto, value);
    }

    public int SelectedCount
    {
        get => _selectedCount;
        set => SetField(ref _selectedCount, value);
    }

    public bool HasSelectedPhotos => SelectedCount > 0;

    public ICommand SelectCommand { get; }
    public ICommand RetakeCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand ContinueToPaymentCommand { get; }
    public ICommand CancelCommand { get; }

    private void SelectPhoto(PhotoItem? photo)
    {
        if (photo != null)
        {
            photo.IsSelected = !photo.IsSelected;
            UpdateSelectedCount();
        }
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Photos.Count(p => p.IsSelected);
        ((RelayCommand)PrintCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ContinueToPaymentCommand).RaiseCanExecuteChanged();
    }

    private void OnPrint()
    {
        // Print selected photos
    }

    public override void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        // Load photos from session
        for (int i = 0; i < 4; i++)
        {
            Photos.Add(new PhotoItem 
            { 
                Id = Guid.NewGuid(), 
                OrderIndex = i, 
                IsSelected = true 
            });
        }
        UpdateSelectedCount();
    }
}
