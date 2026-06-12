using Microsoft.Extensions.DependencyInjection;
using CorraStudio.Application.StateMachine;
using CorraStudio.Application.StateMachine.Services;
using CorraStudio.Application.SessionEngine;
using System.Reflection;

namespace CorraStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // State Machine
        services.AddSingleton<ISessionStateMachine, SessionStateMachine>();
        services.AddScoped<ISessionStateManager, SessionStateManager>();
        
        // Session Engine
        services.AddSingleton<ISessionEngine, SessionEngine>();
        
        return services;
    }
}
