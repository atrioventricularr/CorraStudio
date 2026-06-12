using MediatR;
using CorraStudio.Application.Admin.DTOs;

namespace CorraStudio.Application.Admin.Queries;

public class GetDashboardDataQuery : IRequest<ApiResponse<DashboardDataDto>>
{
    public Guid TenantId { get; set; }
}

public class GetDashboardDataQueryHandler : IRequestHandler<GetDashboardDataQuery, ApiResponse<DashboardDataDto>>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPrintJobRepository _printJobRepository;
    private readonly IPaymentRepository _paymentRepository;

    public GetDashboardDataQueryHandler(
        ISessionRepository sessionRepository,
        IPhotoRepository photoRepository,
        IPrintJobRepository printJobRepository,
        IPaymentRepository paymentRepository)
    {
        _sessionRepository = sessionRepository;
        _photoRepository = photoRepository;
        _printJobRepository = printJobRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<ApiResponse<DashboardDataDto>> Handle(GetDashboardDataQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddMonths(-1);
        
        var dashboard = new DashboardDataDto
        {
            TodayStats = await GetTodayStats(request.TenantId, today),
            WeeklyStats = await GetWeeklyStats(request.TenantId, weekAgo, today),
            MonthlyStats = await GetMonthlyStats(request.TenantId, monthAgo, today),
            RecentActivities = await GetRecentActivities(request.TenantId),
            TopProducts = await GetTopProducts(request.TenantId)
        };
        
        return ApiResponse<DashboardDataDto>.Ok(dashboard);
    }

    private async Task<DailyStatsDto> GetTodayStats(Guid tenantId, DateTime today)
    {
        var sessions = await _sessionRepository.GetByTenantAsync(tenantId);
        var todaySessions = sessions.Where(s => s.CreatedAt.Date == today);
        
        return new DailyStatsDto
        {
            Sessions = todaySessions.Count(),
            Photos = todaySessions.Sum(s => s.PhotoCount),
            Revenue = todaySessions.Sum(s => s.TotalAmount?.Amount ?? 0),
            Prints = 0 // Would need to query print jobs
        };
    }

    private async Task<WeeklyStatsDto> GetWeeklyStats(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        var sessions = await _sessionRepository.GetByTenantAsync(tenantId);
        var weekSessions = sessions.Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate);
        
        var stats = new WeeklyStatsDto
        {
            TotalSessions = weekSessions.Count(),
            TotalRevenue = weekSessions.Sum(s => s.TotalAmount?.Amount ?? 0),
            AveragePerDay = weekSessions.Count() / 7.0,
            DailyBreakdown = new List<DailyStatsDto>()
        };
        
        for (int i = 0; i < 7; i++)
        {
            var day = startDate.AddDays(i);
            var daySessions = weekSessions.Where(s => s.CreatedAt.Date == day);
            
            stats.DailyBreakdown.Add(new DailyStatsDto
            {
                Sessions = daySessions.Count(),
                Photos = daySessions.Sum(s => s.PhotoCount),
                Revenue = daySessions.Sum(s => s.TotalAmount?.Amount ?? 0)
            });
        }
        
        return stats;
    }

    private async Task<MonthlyStatsDto> GetMonthlyStats(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        var sessions = await _sessionRepository.GetByTenantAsync(tenantId);
        var monthSessions = sessions.Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate);
        
        return new MonthlyStatsDto
        {
            TotalSessions = monthSessions.Count(),
            TotalRevenue = monthSessions.Sum(s => s.TotalAmount?.Amount ?? 0),
            AveragePerDay = monthSessions.Count() / 30.0,
            TotalUniqueCustomers = monthSessions.Select(s => s.CustomerEmail?.Value).Distinct().Count()
        };
    }

    private async Task<List<RecentActivityDto>> GetRecentActivities(Guid tenantId)
    {
        var sessions = await _sessionRepository.GetByTenantAsync(tenantId);
        var recentSessions = sessions.OrderByDescending(s => s.CreatedAt).Take(10);
        
        return recentSessions.Select(s => new RecentActivityDto
        {
            Timestamp = s.CreatedAt,
            ActivityType = "Session",
            Description = $"Session {s.SessionCode} completed",
            SessionCode = s.SessionCode
        }).ToList();
    }

    private async Task<List<TopProductDto>> GetTopProducts(Guid tenantId)
    {
        // Mock data for now
        return new List<TopProductDto>
        {
            new TopProductDto { Name = "4x6 Photo Print", Count = 150, Revenue = 750000 },
            new TopProductDto { Name = "Photo Strip", Count = 80, Revenue = 400000 },
            new TopProductDto { Name = "GIF Animation", Count = 45, Revenue = 225000 }
        };
    }
}
