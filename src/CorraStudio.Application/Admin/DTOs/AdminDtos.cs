namespace CorraStudio.Application.Admin.DTOs;

public class SystemSettingsDto
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateSystemSettingsDto
{
    public Dictionary<string, string> Settings { get; set; } = new();
}

public class BackupInfoDto
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class ReportRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ReportType { get; set; }
    public string? Format { get; set; } = "pdf";
}

public class ReportResultDto
{
    public string FilePath { get; set; } = string.Empty;
    public byte[]? Data { get; set; }
    public long FileSizeBytes { get; set; }
    public int TotalRecords { get; set; }
}

public class SystemHealthDto
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

public class ComponentHealth
{
    public string Name { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

public class UserManagementDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Operator";
}

public class UpdateUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ChangePasswordDto
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class DashboardDataDto
{
    public DailyStatsDto TodayStats { get; set; } = new();
    public WeeklyStatsDto WeeklyStats { get; set; } = new();
    public MonthlyStatsDto MonthlyStats { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
}

public class DailyStatsDto
{
    public int Sessions { get; set; }
    public int Photos { get; set; }
    public decimal Revenue { get; set; }
    public int Prints { get; set; }
}

public class WeeklyStatsDto
{
    public int TotalSessions { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AveragePerDay { get; set; }
    public List<DailyStatsDto> DailyBreakdown { get; set; } = new();
}

public class MonthlyStatsDto
{
    public int TotalSessions { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AveragePerDay { get; set; }
    public int TotalUniqueCustomers { get; set; }
}

public class RecentActivityDto
{
    public DateTime Timestamp { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? SessionCode { get; set; }
}

public class TopProductDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}
