using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Queries.Dashboard;

public class GetDashboardStatsQuery : IRequest<ApiResponse<DashboardDto>>
{
    public Guid TenantId { get; set; }
}

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, ApiResponse<DashboardDto>>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IPrintJobRepository _printJobRepository;
    private readonly IPhotoRepository _photoRepository;

    public GetDashboardStatsQueryHandler(
        ISessionRepository sessionRepository,
        IPrintJobRepository printJobRepository,
        IPhotoRepository photoRepository)
    {
        _sessionRepository = sessionRepository;
        _printJobRepository = printJobRepository;
        _photoRepository = photoRepository;
    }

    public async Task<ApiResponse<DashboardDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var activeSessions = await _sessionRepository.GetActiveSessionsAsync(request.TenantId);
        var pendingPrintJobs = await _printJobRepository.GetPendingJobsAsync();
        var allPhotos = await _photoRepository.GetByTenantAsync(request.TenantId);

        var dashboard = new DashboardDto
        {
            TotalSessionsToday = await _sessionRepository.GetSessionCountTodayAsync(request.TenantId),
            TotalSessionsThisMonth = await _sessionRepository.GetSessionCountByDateRangeAsync(request.TenantId, startOfMonth, today),
            TotalRevenueToday = await _paymentRepository.GetTotalRevenueByDateAsync(request.TenantId, today),
            TotalRevenueThisMonth = await _paymentRepository.GetTotalRevenueByDateRangeAsync(request.TenantId, startOfMonth, today),
            ActiveSessions = activeSessions.Count(),
            PendingPrintJobs = pendingPrintJobs.Count(),
            TotalPhotosCaptured = allPhotos.Count()
        };

        return ApiResponse<DashboardDto>.Ok(dashboard);
    }
}
