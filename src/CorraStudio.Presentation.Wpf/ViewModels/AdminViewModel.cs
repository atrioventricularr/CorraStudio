using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class AdminViewModel : ViewModelBase
{
    private string _selectedTab = "Dashboard";
    private ObservableCollection<SessionSummary> _recentSessions = new();
    private DashboardStats _stats = new();

    public AdminViewModel()
    {
        RefreshCommand = new RelayCommand(() => OnRefreshRequested?.Invoke());
        ExportReportCommand = new RelayCommand(() => OnExportReportRequested?.Invoke());
        SettingsCommand = new RelayCommand<string>(OpenSettings);
    }

    public string SelectedTab
    {
        get => _selectedTab;
        set => SetField(ref _selectedTab, value);
    }

    public ObservableCollection<SessionSummary> RecentSessions
    {
        get => _recentSessions;
        set => SetField(ref _recentSessions, value);
    }

    public DashboardStats Stats
    {
        get => _stats;
        set => SetField(ref _stats, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand SettingsCommand { get; }

    public event Action? OnRefreshRequested;
    public event Action? OnExportReportRequested;

    private void OpenSettings(string settingsKey)
    {
        // Navigate to specific settings page
    }

    public void UpdateStats(int todaySessions, int totalSessions, decimal todayRevenue, decimal totalRevenue)
    {
        Stats = new DashboardStats
        {
            TodaySessions = todaySessions,
            TotalSessions = totalSessions,
            TodayRevenue = todayRevenue,
            TotalRevenue = totalRevenue
        };
    }

    public void AddRecentSession(SessionSummary session)
    {
        RecentSessions.Insert(0, session);
        if (RecentSessions.Count > 20)
            RecentSessions.RemoveAt(RecentSessions.Count - 1);
    }
}

public class DashboardStats : ViewModelBase
{
    private int _todaySessions;
    private int _totalSessions;
    private decimal _todayRevenue;
    private decimal _totalRevenue;

    public int TodaySessions
    {
        get => _todaySessions;
        set => SetField(ref _todaySessions, value);
    }

    public int TotalSessions
    {
        get => _totalSessions;
        set => SetField(ref _totalSessions, value);
    }

    public decimal TodayRevenue
    {
        get => _todayRevenue;
        set => SetField(ref _todayRevenue, value);
    }

    public decimal TotalRevenue
    {
        get => _totalRevenue;
        set => SetField(ref _totalRevenue, value);
    }
}

public class SessionSummary : ViewModelBase
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int PhotoCount { get; set; }
    public decimal? Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
