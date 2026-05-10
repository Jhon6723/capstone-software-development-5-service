using Microsoft.Extensions.DependencyInjection;
using PixPro.Services.Notifications.Application.Services;

namespace PixPro.Services.Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly)
        );
        
        // Register legacy service (will be removed after full CQRS migration)
        services.AddScoped<INotificationService, NotificationService>();
        
        return services;
    }
}
