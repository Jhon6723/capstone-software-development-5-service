using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PixPro.Services.Notifications.Domain.Repositories;
using PixPro.Services.Notifications.Infrastructure.Messaging;
using PixPro.Services.Notifications.Infrastructure.Persistence;
using PixPro.Services.Notifications.Infrastructure.WebSockets;

namespace PixPro.Services.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB Configuration
        var mongoConnectionString = configuration.GetConnectionString("NotificationsDb") 
            ?? "mongodb://localhost:27017/pixpro_notifications";
        
        var mongoUrl = MongoUrl.Create(mongoConnectionString);
        var mongoClient = new MongoClient(mongoConnectionString);
        
        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return new NotificationDbContext(client, mongoUrl.DatabaseName ?? "pixpro_notifications");
        });

        // Register Repository
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Register WebSocket Services
        services.AddSingleton<WebSocketConnectionManager>();
        services.AddScoped<IWebSocketNotificationService, WebSocketNotificationService>();

        // Register RabbitMQ Consumer as Background Service
        services.AddHostedService<RabbitMqConsumer>();

        return services;
    }
}
