using MediatR;

namespace CorraStudio.Application.Admin.Commands;

public class RestoreBackupCommand : IRequest<ApiResponse<bool>>
{
    public string BackupPath { get; set; } = string.Empty;
}

public class RestoreBackupCommandHandler : IRequestHandler<RestoreBackupCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(RestoreBackupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(request.BackupPath))
                return ApiResponse<bool>.Fail("Backup file not found");
            
            // Simulate restore
            await Task.Delay(3000, cancellationToken);
            
            return ApiResponse<bool>.Ok(true, "Backup restored successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Fail($"Restore failed: {ex.Message}");
        }
    }
}
