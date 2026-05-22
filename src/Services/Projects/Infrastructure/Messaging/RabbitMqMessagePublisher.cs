using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PixPro.Services.Projects.Application.Interfaces;
using RabbitMQ.Client;

namespace PixPro.Services.Projects.Infrastructure.Messaging;

public class RabbitMqMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly ILogger<RabbitMqMessagePublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqMessagePublisher(
        IConfiguration configuration,
        ILogger<RabbitMqMessagePublisher> logger)
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

        // Declare exchange for image events
        _channel.ExchangeDeclare("image-events", ExchangeType.Fanout, durable: true);

        _logger.LogInformation("RabbitMQ publisher connection established");
    }

    public Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        // Publish to exchange instead of direct queue
        _channel.BasicPublish(
            exchange: "image-events",
            routingKey: "",
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published message to exchange 'image-events': {MessageType}",
            typeof(T).Name);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}

