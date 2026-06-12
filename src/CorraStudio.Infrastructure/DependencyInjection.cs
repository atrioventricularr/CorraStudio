using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CorraStudio.Domain.Interfaces.Repositories;
using CorraStudio.Domain.Interfaces.Services;
using CorraStudio.Infrastructure.Repositories;
using CorraStudio.Infrastructure.Services;
using CorraStudio.Infrastructure.Caching;
using CorraStudio.Infrastructure.BackgroundServices;

namespace CorraStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories
        services.AddSingleton<ITenantRepository, TenantRepository>();
        services.AddSingleton<ISessionRepository, SessionRepository>();
        services.AddSingleton<IPhotoRepository, PhotoRepository>();
        services.AddSingleton<IPrintJobRepository, PrintJobRepository>();
        services.AddSingleton<IPaymentRepository, PaymentRepository>();
        services.AddSingleton<ILayoutRepository, LayoutRepository>();
        services.AddSingleton<ITemplateRepository, TemplateRepository>();
        services.AddSingleton<IEventRepository, EventRepository>();
        services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();

        // Services
        services.AddSingleton<IStorageService, FileStorageService>();
        services.AddSingleton<IQrCodeService, QrCodeService>();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        
        // Background Services
        services.AddHostedService<SessionWorkerService>();

        return services;
    }
}
