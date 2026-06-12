namespace CorraStudio.Application.DTOs;

public class DashboardDto
{
    public int TotalSessionsToday { get; set; }
    public int TotalSessionsThisMonth { get; set; }
    public decimal TotalRevenueToday { get; set; }
    public decimal TotalRevenueThisMonth { get; set; }
    public int ActiveSessions { get; set; }
    public int PendingPrintJobs { get; set; }
    public int TotalPhotosCaptured { get; set; }
    public List<DailyStatDto> Last7DaysStats { get; set; } = new();
}

public class DailyStatDto
{
    public DateTime Date { get; set; }
    public int SessionCount { get; set; }
    public decimal Revenue { get; set; }
}

public class SessionStatDto
{
    public Guid SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public int PhotoCount { get; set; }
    public decimal? Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
