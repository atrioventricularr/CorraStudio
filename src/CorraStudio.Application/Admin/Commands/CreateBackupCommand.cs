using MediatR;
using CorraStudio.Application.Admin.DTOs;

namespace CorraStudio.Application.Admin.Commands;

public class CreateBackupCommand : IRequest<ApiResponse<BackupInfoDto>>
{
    public Guid TenantId { get; set; }
    public string? BackupPath { get; set; }
    public bool IncludePhotos { get; set; } = true;
}

public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, ApiResponse<BackupInfoDto>>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IConfigurationRepository _configurationRepository;

    public CreateBackupCommandHandler(
        ISessionRepository sessionRepository,
        IPhotoRepository photoRepository,
        IConfigurationRepository configurationRepository)
    {
        _sessionRepository = sessionRepository;
        _photoRepository = photoRepository;
        _configurationRepository = configurationRepository;
    }

    public async Task<ApiResponse<BackupInfoDto>> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var backupDir = request.BackupPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CorraStudio", "Backups");
            
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"backup_{timestamp}.zip";
            var backupFilePath = Path.Combine(backupDir, backupFileName);
            
            // Simulate backup creation
            await Task.Delay(2000, cancellationToken);
            
            var backupInfo = new BackupInfoDto
            {
                FileName = backupFileName,
                FilePath = backupFilePath,
                FileSizeBytes = 1024 * 1024, // 1MB mock
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            
            return ApiResponse<BackupInfoDto>.Ok(backupInfo, "Backup created successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<BackupInfoDto>.Fail($"Backup failed: {ex.Message}");
        }
    }
}
