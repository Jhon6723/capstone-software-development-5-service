using Microsoft.Extensions.DependencyInjection;
using PixPro.Services.Notifications.Application.Services;

namespace PixPro.Services.Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        
        return services;
    }
}
