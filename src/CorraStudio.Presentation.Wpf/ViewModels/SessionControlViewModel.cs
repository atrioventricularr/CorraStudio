using System.Collections.ObjectModel;
using System.Windows.Input;
using CorraStudio.Application.SessionEngine;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class SessionControlViewModel : ViewModelBase
{
    private readonly ISessionEngine _sessionEngine;
    private ObservableCollection<SessionInfo> _activeSessions = new();
    private ObservableCollection<QueueItemInfo> _queueItems = new();
    private SessionStatistics _statistics = new();
    private SessionInfo? _selectedSession;
    private string _statusFilter = "All";
    private int _queueLength;
    private bool _isProcessing;

    public SessionControlViewModel(ISessionEngine sessionEngine)
    {
        _sessionEngine = sessionEngine;
        
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        CreateSessionCommand = new RelayCommand(async () => await CreateSessionAsync());
        CancelSessionCommand = new RelayCommand(async () => await CancelSelectedSessionAsync(), () => SelectedSession != null);
        ProcessQueueCommand = new RelayCommand(async () => await ProcessQueueAsync(), () => !IsProcessing);
        ClearQueueCommand = new RelayCommand(async () => await ClearQueueAsync());
        
        // Subscribe to events
        _sessionEngine.SessionCreated += OnSessionCreated;
        _sessionEngine.SessionStarted += OnSessionStarted;
        _sessionEngine.SessionCompleted += OnSessionCompleted;
        _sessionEngine.SessionCancelled += OnSessionCancelled;
        _sessionEngine.QueueItemAdded += OnQueueItemAdded;
        _sessionEngine.QueueItemProcessed += OnQueueItemProcessed;
        _sessionEngine.QueueItemFailed += OnQueueItemFailed;
        
        // Initial refresh
        Task.Run(async () => await RefreshAsync());
        
        // Auto-refresh every 5 seconds
        var timer = new System.Timers.Timer(5000);
        timer.Elapsed += async (s, e) => await RefreshAsync();
        timer.Start();
    }

    public ObservableCollection<SessionInfo> ActiveSessions
    {
        get => _activeSessions;
        set => SetField(ref _activeSessions, value);
    }

    public ObservableCollection<QueueItemInfo> QueueItems
    {
        get => _queueItems;
        set => SetField(ref _queueItems, value);
    }

    public SessionStatistics Statistics
    {
        get => _statistics;
        set => SetField(ref _statistics, value);
    }

    public SessionInfo? SelectedSession
    {
        get => _selectedSession;
        set => SetField(ref _selectedSession, value);
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            SetField(ref _statusFilter, value);
            Task.Run(async () => await RefreshQueueItemsAsync());
        }
    }

    public int QueueLength
    {
        get => _queueLength;
        set => SetField(ref _queueLength, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set => SetField(ref _isProcessing, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateSessionCommand { get; }
    public ICommand CancelSessionCommand { get; }
    public ICommand ProcessQueueCommand { get; }
    public ICommand ClearQueueCommand { get; }

    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            
            // Refresh active sessions
            var sessions = _sessionEngine.GetActiveSessions();
            ActiveSessions.Clear();
            
            foreach (var session in sessions)
            {
                ActiveSessions.Add(new SessionInfo
                {
                    Id = session.Id,
                    Code = session.SessionCode,
                    Status = session.Status.ToString(),
                    PhotoCount = session.PhotoCount,
                    StartedAt = session.StartedAt,
                    TenantId = session.TenantId ?? Guid.Empty
                });
            }
            
            // Refresh statistics
            if (ActiveSessions.Count > 0)
            {
                var stats = await _sessionEngine.GetStatisticsAsync(ActiveSessions[0].TenantId);
                Statistics = stats;
            }
            
            // Refresh queue items
            await RefreshQueueItemsAsync();
            
            QueueLength = await _sessionEngine.GetQueueLengthAsync();
        }
        catch (Exception ex)
        {
            SetError($"Refresh failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshQueueItemsAsync()
    {
        var items = await _sessionEngine.GetPendingQueueItemsAsync();
        QueueItems.Clear();
        
        var filtered = StatusFilter == "All" 
            ? items 
            : items.Where(i => i.Status.ToString() == StatusFilter);
        
        foreach (var item in filtered)
        {
            QueueItems.Add(new QueueItemInfo
            {
                Id = item.Id,
                SessionId = item.SessionId,
                Type = item.Type.ToString(),
                Status = item.Status.ToString(),
                CreatedAt = item.CreatedAt,
                RetryCount = item.RetryCount,
                ErrorMessage = item.ErrorMessage
            });
        }
    }

    private async Task CreateSessionAsync()
    {
        try
        {
            var session = await _sessionEngine.CreateSessionAsync(Guid.NewGuid());
            await _sessionEngine.StartSessionAsync(session.Id);
            StatusMessage = $"Session {session.SessionCode} created";
        }
        catch (Exception ex)
        {
            SetError($"Create session failed: {ex.Message}");
        }
    }

    private async Task CancelSelectedSessionAsync()
    {
        if (SelectedSession == null) return;
        
        var confirmed = await DialogService.ShowConfirmationAsync(
            $"Cancel session {SelectedSession.Code}?", 
            "Confirm Cancel");
        
        if (confirmed)
        {
            await _sessionEngine.CancelSessionAsync(SelectedSession.Id, "User cancelled");
            StatusMessage = $"Session {SelectedSession.Code} cancelled";
        }
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            IsProcessing = true;
            var processed = await _sessionEngine.ProcessQueueAsync();
            StatusMessage = $"Processed {processed} queue items";
            await RefreshQueueItemsAsync();
        }
        catch (Exception ex)
        {
            SetError($"Process queue failed: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task ClearQueueAsync()
    {
        var items = await _sessionEngine.GetPendingQueueItemsAsync();
        foreach (var item in items)
        {
            await _sessionEngine.CancelQueueItemAsync(item.Id);
        }
        StatusMessage = $"Cleared {items.Count} queue items";
        await RefreshQueueItemsAsync();
    }

    private void OnSessionCreated(object? sender, SessionEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            StatusMessage = $"Session {e.SessionCode} created";
            Task.Run(async () => await RefreshAsync());
        });
    }

    private void OnSessionStarted(object? sender, SessionEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            StatusMessage = $"Session {e.SessionCode} started";
        });
    }

    private void OnSessionCompleted(object? sender, SessionEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            StatusMessage = $"Session {e.SessionCode} completed";
            Task.Run(async () => await RefreshAsync());
        });
    }

    private void OnSessionCancelled(object? sender, SessionEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            StatusMessage = $"Session {e.SessionCode} cancelled";
            Task.Run(async () => await RefreshAsync());
        });
    }

    private void OnQueueItemAdded(object? sender, QueueItemEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            QueueLength++;
            Task.Run(async () => await RefreshQueueItemsAsync());
        });
    }

    private void OnQueueItemProcessed(object? sender, QueueItemEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            QueueLength--;
            Task.Run(async () => await RefreshQueueItemsAsync());
        });
    }

    private void OnQueueItemFailed(object? sender, QueueItemEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            StatusMessage = $"Queue item failed: {e.ErrorMessage}";
            Task.Run(async () => await RefreshQueueItemsAsync());
        });
    }
}

public class SessionInfo
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PhotoCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public Guid TenantId { get; set; }
}

public class QueueItemInfo
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
