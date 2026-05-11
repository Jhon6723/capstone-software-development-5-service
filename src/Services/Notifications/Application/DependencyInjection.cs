using Microsoft.Extensions.DependencyInjection;

namespace PixPro.Services.Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR for CQRS pattern
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly)
        );
        
        return services;
    }
}
