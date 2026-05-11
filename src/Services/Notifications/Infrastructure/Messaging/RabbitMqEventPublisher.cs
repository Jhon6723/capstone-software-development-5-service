using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Events;
using RabbitMQ.Client;

namespace PixPro.Services.Notifications.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of event publisher for CQRS pattern
/// Publishes domain events to "notification-domain-events" queue for read database synchronization
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "notification-domain-events";
    private const string QueueName = "notification-domain-events";

    public RabbitMqEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange and queue for domain events
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Bind queue to exchange with routing key pattern
        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: "notification.*");

        _logger.LogInformation("RabbitMQ Event Publisher initialized");
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : NotificationDomainEvent
    {
        try
        {
            var eventType = @event.GetType().Name;
            var routingKey = $"notification.{eventType.ToLowerInvariant()}";
            
            var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Type = eventType;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published domain event {EventType} with EventId {EventId} to RabbitMQ",
                eventType,
                @event.EventId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing domain event {EventType}", typeof(TEvent).Name);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
