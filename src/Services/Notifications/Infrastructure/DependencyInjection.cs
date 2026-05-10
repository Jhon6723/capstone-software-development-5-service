using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Repositories;
using PixPro.Services.Notifications.Infrastructure.Messaging;
using PixPro.Services.Notifications.Infrastructure.Persistence;
using PixPro.Services.Notifications.Infrastructure.WebSockets;
using StackExchange.Redis;

namespace PixPro.Services.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB Configuration (Write Database)
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

        // Redis Configuration (Read Database for CQRS)
        var redisConnectionString = configuration.GetConnectionString("Redis") 
            ?? "localhost:6379";
        
        // Add password if configured
        var redisPassword = configuration["Redis:Password"];
        if (!string.IsNullOrEmpty(redisPassword))
        {
            redisConnectionString += $",password={redisPassword}";
        }
        
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            return connectionMultiplexer;
        });

        // Register Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>(); // Write operations
        services.AddScoped<INotificationReadRepository, RedisNotificationReadRepository>(); // Read operations

        // Register Event Publisher for CQRS
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // Register WebSocket Services
        services.AddSingleton<WebSocketConnectionManager>();
        services.AddScoped<IWebSocketNotificationService, WebSocketNotificationService>();

        // Register Background Service
        // RabbitMqConsumer handles:
        // 1. External events (user-events, project-events, image-processing-events, notifications)
        // 2. Internal domain events (notification-domain-events) for CQRS sync to Redis
        services.AddHostedService<RabbitMqConsumer>();

        return services;
    }
}
