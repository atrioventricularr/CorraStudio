using System.Collections.ObjectModel;
using System.Windows.Input;
using CorraStudio.Sync.CloudSync;
using CorraStudio.Sync.Queue;

namespace CorraStudio.Presentation.Wpf.ViewModels.Admin;

public class CloudSyncViewModel : ViewModelBase
{
    private readonly ICloudSyncService _cloudSyncService;
    private readonly ISyncQueueManager _syncQueueManager;
    
    private SyncStatistics _statistics = new();
    private ObservableCollection<SyncQueueItemSummary> _queueItems = new();
    private ObservableCollection<CloudDevice> _connectedDevices = new();
    private bool _isAutoSyncEnabled = true;
    private int _syncIntervalSeconds = 60;
    private bool _isSyncing;
    private string _syncStatus = "Ready";
    private bool _isCloudConnected;

    public CloudSyncViewModel(ICloudSyncService cloudSyncService, ISyncQueueManager syncQueueManager)
    {
        _cloudSyncService = cloudSyncService;
        _syncQueueManager = syncQueueManager;
        
        SyncNowCommand = new RelayCommand(async () => await SyncNowAsync(), () => !IsSyncing && IsCloudConnected);
        RetryFailedCommand = new RelayCommand(async () => await RetryFailedAsync(), () => !IsSyncing);
        ClearQueueCommand = new RelayCommand(async () => await ClearQueueAsync(), () => !IsSyncing);
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        ToggleAutoSyncCommand = new RelayCommand(async () => await ToggleAutoSyncAsync());
        
        _cloudSyncService.SyncProgress += OnSyncProgress;
        _cloudSyncService.SyncCompleted += OnSyncCompleted;
        
        Task.Run(InitializeAsync);
        Task.Run(MonitorCloudStatusAsync);
        Task.Run(RefreshLoopAsync);
    }

    public SyncStatistics Statistics
    {
        get => _statistics;
        set => SetField(ref _statistics, value);
    }

    public ObservableCollection<SyncQueueItemSummary> QueueItems
    {
        get => _queueItems;
        set => SetField(ref _queueItems, value);
    }

    public ObservableCollection<CloudDevice> ConnectedDevices
    {
        get => _connectedDevices;
        set => SetField(ref _connectedDevices, value);
    }

    public bool IsAutoSyncEnabled
    {
        get => _isAutoSyncEnabled;
        set => SetField(ref _isAutoSyncEnabled, value);
    }

    public int SyncIntervalSeconds
    {
        get => _syncIntervalSeconds;
        set => SetField(ref _syncIntervalSeconds, value);
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

    public string SyncStatus
    {
        get => _syncStatus;
        set => SetField(ref _syncStatus, value);
    }

    public bool IsCloudConnected
    {
        get => _isCloudConnected;
        set
        {
            SetField(ref _isCloudConnected, value);
            ((RelayCommand)SyncNowCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand SyncNowCommand { get; }
    public ICommand RetryFailedCommand { get; }
    public ICommand ClearQueueCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ToggleAutoSyncCommand { get; }

    private async Task InitializeAsync()
    {
        var config = new CloudSyncConfig
        {
            EnableAutoSync = IsAutoSyncEnabled,
            SyncIntervalSeconds = SyncIntervalSeconds,
            SyncOnStartup = true,
            SyncOnSessionComplete = true
        };
        
        await _cloudSyncService.InitializeAsync(config);
    }

    private async Task MonitorCloudStatusAsync()
    {
        while (true)
        {
            IsCloudConnected = await _cloudSyncService.IsCloudConnectedAsync();
            await Task.Delay(10000);
        }
    }

    private async Task RefreshLoopAsync()
    {
        while (true)
        {
            await RefreshAsync();
            await Task.Delay(5000);
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            Statistics = await _cloudSyncService.GetStatisticsAsync(Guid.Empty);
            var items = await _syncQueueManager.GetQueueItemsAsync();
            
            App.Current?.Dispatcher.Invoke(() =>
            {
                QueueItems.Clear();
                foreach (var item in items)
                {
                    QueueItems.Add(item);
                }
            });
            
            var devices = await _cloudSyncService.GetConnectedDevicesAsync(Guid.Empty);
            App.Current?.Dispatcher.Invoke(() =>
            {
                ConnectedDevices.Clear();
                foreach (var device in devices)
                {
                    ConnectedDevices.Add(device);
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Refresh failed");
        }
    }

    private async Task SyncNowAsync()
    {
        try
        {
            IsSyncing = true;
            SyncStatus = "Starting sync...";
            
            var result = await _cloudSyncService.SyncNowAsync();
            
            if (result.Success)
            {
                SyncStatus = $"Synced {result.ItemsSynced} items";
            }
            else
            {
                SyncStatus = $"Sync failed: {result.ErrorMessage}";
            }
            
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            SyncStatus = $"Sync error: {ex.Message}";
            SetError(ex.Message);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task RetryFailedAsync()
    {
        try
        {
            IsLoading = true;
            await _syncQueueManager.RetryFailedItemsAsync();
            SyncStatus = "Retrying failed items...";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            SetError($"Failed to retry: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearQueueAsync()
    {
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Clear all pending sync items?", 
            "Confirm Clear");
        
        if (confirmed)
        {
            await _syncQueueManager.ClearQueueAsync();
            SyncStatus = "Queue cleared";
            await RefreshAsync();
        }
    }

    private async Task ToggleAutoSyncAsync()
    {
        // Save setting to database
        await Task.CompletedTask;
    }

    private void OnSyncProgress(object? sender, SyncProgressEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            SyncStatus = $"Syncing {e.CurrentEntity} ({e.CurrentItem}/{e.TotalItems})";
            Progress = (e.CurrentItem * 100) / e.TotalItems;
        });
    }

    private void OnSyncCompleted(object? sender, SyncCompletedEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            if (e.Success)
            {
                SyncStatus = $"Sync completed: {e.ItemsSynced} items synced";
            }
            else
            {
                SyncStatus = $"Sync completed with {e.FailedItems} failures";
            }
            
            Progress = 0;
        });
    }
}
