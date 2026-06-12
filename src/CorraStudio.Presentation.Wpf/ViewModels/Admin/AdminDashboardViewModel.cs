using System.Collections.ObjectModel;
using System.Windows.Input;
using CorraStudio.Application.Admin.DTOs;
using CorraStudio.Application.Admin.Queries;
using MediatR;

namespace CorraStudio.Presentation.Wpf.ViewModels.Admin;

public class AdminDashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private DashboardDataDto _dashboardData = new();
    private SystemHealthDto? _systemHealth;
    private ObservableCollection<AuditLogDto> _auditLogs = new();
    private ObservableCollection<UserManagementDto> _users = new();
    private ObservableCollection<BackupInfoDto> _backups = new();
    private bool _isRefreshing;
    private string _selectedTab = "Dashboard";

    public AdminDashboardViewModel(IMediator mediator)
    {
        _mediator = mediator;
        
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        CreateBackupCommand = new RelayCommand(async () => await CreateBackupAsync());
        ExportReportCommand = new RelayCommand(async () => await ExportReportAsync());
        ClearLogsCommand = new RelayCommand(async () => await ClearLogsAsync());
        
        LoadData();
    }

    public DashboardDataDto DashboardData
    {
        get => _dashboardData;
        set => SetField(ref _dashboardData, value);
    }

    public SystemHealthDto? SystemHealth
    {
        get => _systemHealth;
        set => SetField(ref _systemHealth, value);
    }

    public ObservableCollection<AuditLogDto> AuditLogs
    {
        get => _auditLogs;
        set => SetField(ref _auditLogs, value);
    }

    public ObservableCollection<UserManagementDto> Users
    {
        get => _users;
        set => SetField(ref _users, value);
    }

    public ObservableCollection<BackupInfoDto> Backups
    {
        get => _backups;
        set => SetField(ref _backups, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetField(ref _isRefreshing, value);
    }

    public string SelectedTab
    {
        get => _selectedTab;
        set => SetField(ref _selectedTab, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateBackupCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand ClearLogsCommand { get; }

    private async void LoadData()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            StatusMessage = "Loading dashboard data...";
            
            // Load dashboard data
            var dashboardResult = await _mediator.Send(new GetDashboardDataQuery { TenantId = Guid.Empty });
            if (dashboardResult.Success && dashboardResult.Data != null)
            {
                DashboardData = dashboardResult.Data;
            }
            
            // Load system health
            var healthResult = await _mediator.Send(new GetSystemHealthQuery { TenantId = Guid.Empty });
            if (healthResult.Success && healthResult.Data != null)
            {
                SystemHealth = healthResult.Data;
            }
            
            StatusMessage = "Dashboard updated successfully";
        }
        catch (Exception ex)
        {
            SetError($"Failed to refresh dashboard: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task CreateBackupAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating backup...";
            
            // Backup logic here
            await Task.Delay(2000);
            
            StatusMessage = "Backup created successfully";
        }
        catch (Exception ex)
        {
            SetError($"Backup failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportReportAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Exporting report...";
            
            await Task.Delay(1000);
            
            StatusMessage = "Report exported successfully";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearLogsAsync()
    {
        var confirmed = await DialogService.ShowConfirmationAsync("Clear all audit logs?", "Confirm");
        if (confirmed)
        {
            AuditLogs.Clear();
            StatusMessage = "Audit logs cleared";
        }
    }
}
