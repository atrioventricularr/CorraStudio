using System.Windows.Input;
using System.Windows.Media.Imaging;
using CorraStudio.Sync.Services;

namespace CorraStudio.Presentation.Wpf.ViewModels.Gallery;

public class GallerySyncViewModel : ViewModelBase
{
    private readonly IGallerySyncManager _syncManager;
    private readonly IGalleryQrService _qrService;
    
    private BitmapImage? _qrCode;
    private string _galleryUrl = string.Empty;
    private string _syncStatus = "Ready";
    private int _syncProgress;
    private int _pendingSyncs;
    private bool _isCloudAvailable;
    private bool _isSyncing;

    public GallerySyncViewModel(IGallerySyncManager syncManager, IGalleryQrService qrService)
    {
        _syncManager = syncManager;
        _qrService = qrService;
        
        SyncNowCommand = new RelayCommand(async () => await SyncNowAsync(), () => !IsSyncing);
        OpenGalleryCommand = new RelayCommand(() => OpenGallery());
        CopyLinkCommand = new RelayCommand(() => CopyGalleryLink());
        
        _syncManager.SyncProgress += OnSyncProgress;
        _syncManager.SyncCompleted += OnSyncCompleted;
        
        Task.Run(InitializeAsync);
        Task.Run(MonitorCloudStatusAsync);
    }

    public BitmapImage? QrCode
    {
        get => _qrCode;
        set => SetField(ref _qrCode, value);
    }

    public string GalleryUrl
    {
        get => _galleryUrl;
        set => SetField(ref _galleryUrl, value);
    }

    public string SyncStatus
    {
        get => _syncStatus;
        set => SetField(ref _syncStatus, value);
    }

    public int SyncProgress
    {
        get => _syncProgress;
        set => SetField(ref _syncProgress, value);
    }

    public int PendingSyncs
    {
        get => _pendingSyncs;
        set => SetField(ref _pendingSyncs, value);
    }

    public bool IsCloudAvailable
    {
        get => _isCloudAvailable;
        set => SetField(ref _isCloudAvailable, value);
    }

    public bool IsSyncing
    {
        get => _isSyncing;
        set
        {
            SetField(ref _isSyncing, value);
            ((RelayCommand)SyncNowCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand SyncNowCommand { get; }
    public ICommand OpenGalleryCommand { get; }
    public ICommand CopyLinkCommand { get; }

    private async Task InitializeAsync()
    {
        await _syncManager.InitializeAsync();
        await UpdatePendingSyncsAsync();
    }

    private async Task MonitorCloudStatusAsync()
    {
        while (true)
        {
            IsCloudAvailable = await _syncManager.IsCloudAvailableAsync();
            await Task.Delay(30000); // Check every 30 seconds
        }
    }

    private async Task UpdatePendingSyncsAsync()
    {
        PendingSyncs = await _syncManager.GetPendingSyncCountAsync();
    }

    private async Task SyncNowAsync()
    {
        try
        {
            IsSyncing = true;
            SyncStatus = "Starting sync...";
            
            await _syncManager.ProcessPendingSyncsAsync();
            
            await UpdatePendingSyncsAsync();
        }
        catch (Exception ex)
        {
            SyncStatus = $"Sync failed: {ex.Message}";
            SetError(ex.Message);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private void OnSyncProgress(object? sender, SyncProgressEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            SyncProgress = (e.CurrentPhoto * 100) / e.TotalPhotos;
            SyncStatus = e.Status;
        });
    }

    private void OnSyncCompleted(object? sender, SyncCompletedEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            if (e.Success)
            {
                GalleryUrl = e.GalleryUrl;
                SyncStatus = "Sync completed!";
                GenerateQrCode(e.GalleryUrl);
            }
            else
            {
                SyncStatus = $"Sync failed: {e.ErrorMessage}";
            }
        });
    }

    private async void GenerateQrCode(string url)
    {
        try
        {
            var sessionCode = Guid.NewGuid().ToString().Substring(0, 8);
            var qrData = await _qrService.GenerateGalleryQrCodeAsync(url, sessionCode);
            
            using var stream = new MemoryStream(qrData);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            
            QrCode = bitmap;
        }
        catch (Exception ex)
        {
            SetError($"QR generation failed: {ex.Message}");
        }
    }

    private void OpenGallery()
    {
        if (!string.IsNullOrEmpty(GalleryUrl))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = GalleryUrl,
                UseShellExecute = true
            });
        }
    }

    private void CopyGalleryLink()
    {
        if (!string.IsNullOrEmpty(GalleryUrl))
        {
            System.Windows.Clipboard.SetText(GalleryUrl);
            SyncStatus = "Link copied to clipboard!";
        }
    }

    public override void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        Task.Run(UpdatePendingSyncsAsync);
    }
}
