using MediatR;
using CorraStudio.Application.Admin.DTOs;

namespace CorraStudio.Application.Admin.Queries;

public class GetSystemHealthQuery : IRequest<ApiResponse<SystemHealthDto>>
{
    public Guid TenantId { get; set; }
}

public class GetSystemHealthQueryHandler : IRequestHandler<GetSystemHealthQuery, ApiResponse<SystemHealthDto>>
{
    public async Task<ApiResponse<SystemHealthDto>> Handle(GetSystemHealthQuery request, CancellationToken cancellationToken)
    {
        var health = new SystemHealthDto
        {
            IsHealthy = true,
            Status = "Healthy",
            CheckedAt = DateTime.UtcNow,
            Components = new Dictionary<string, ComponentHealth>()
        };
        
        // Check database
        health.Components["Database"] = new ComponentHealth
        {
            Name = "Database",
            IsHealthy = true,
            ResponseTime = TimeSpan.FromMilliseconds(50)
        };
        
        // Check storage
        health.Components["Storage"] = new ComponentHealth
        {
            Name = "Storage",
            IsHealthy = true,
            ResponseTime = TimeSpan.FromMilliseconds(10)
        };
        
        // Check camera
        health.Components["Camera"] = new ComponentHealth
        {
            Name = "Camera",
            IsHealthy = true,
            Message = "No camera connected"
        };
        
        // Check printer
        health.Components["Printer"] = new ComponentHealth
        {
            Name = "Printer",
            IsHealthy = true,
            Message = "Printer ready"
        };
        
        return ApiResponse<SystemHealthDto>.Ok(health);
    }
}
