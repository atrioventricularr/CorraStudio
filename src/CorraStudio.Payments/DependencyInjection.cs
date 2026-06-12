using Microsoft.Extensions.DependencyInjection;
using CorraStudio.Payments.Abstractions;
using CorraStudio.Payments.Services;
using CorraStudio.Payments.Gateways.QRIS;

namespace CorraStudio.Payments;

public static class DependencyInjection
{
    public static IServiceCollection AddPayments(this IServiceCollection services)
    {
        // Services
        services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddSingleton<IPaymentService, PaymentService>();
        
        // QRIS Static
        services.AddSingleton<QrisStaticGateway>();
        
        return services;
    }
}
