using MediatR;
using CorraStudio.Application.Admin.DTOs;
using CorraStudio.Domain.Interfaces.Repositories;

namespace CorraStudio.Application.Admin.Commands;

public class UpdateSystemSettingsCommand : IRequest<ApiResponse<bool>>
{
    public Guid TenantId { get; set; }
    public UpdateSystemSettingsDto Settings { get; set; } = new();
}

public class UpdateSystemSettingsCommandHandler : IRequestHandler<UpdateSystemSettingsCommand, ApiResponse<bool>>
{
    private readonly IConfigurationRepository _configurationRepository;

    public UpdateSystemSettingsCommandHandler(IConfigurationRepository configurationRepository)
    {
        _configurationRepository = configurationRepository;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateSystemSettingsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var setting in request.Settings.Settings)
            {
                await _configurationRepository.SetValueAsync(request.TenantId, setting.Key, setting.Value, "System");
            }
            
            return ApiResponse<bool>.Ok(true, "Settings updated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Fail($"Failed to update settings: {ex.Message}");
        }
    }
}
