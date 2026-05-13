using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PixPro.Services.Projects.Application.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PixPro.Services.Projects.Infrastructure.Messaging;

public sealed class RabbitMqImageProcessingConsumer : BackgroundService
{
    private readonly ILogger<RabbitMqImageProcessingConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    private const string QueueName = "image-processing-events";

    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqImageProcessingConsumer(
        ILogger<RabbitMqImageProcessingConsumer> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);

        try
        {
            InitializeRabbitMq();
            ConsumeQueue(stoppingToken);

            _logger.LogInformation("Image Processing Consumer started — listening on queue '{Queue}'", QueueName);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Image Processing Consumer");
        }
    }

    private void InitializeRabbitMq()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("RabbitMQ connection established for Image Processing Consumer");
    }

    private void ConsumeQueue(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received image-processing message: {Message}", messageJson);

                var message = JsonSerializer.Deserialize<ImageProcessingMessage>(messageJson);

                if (message is null)
                {
                    _logger.LogWarning("Could not deserialize message, skipping");
                    _channel?.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                using (var scope = _serviceProvider.CreateScope())
                {
                    var processingService = scope.ServiceProvider
                        .GetRequiredService<IImageProcessingService>();

                    await processingService.ProcessAsync(message.ImageId, stoppingToken);
                }

                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Image {ImageId} processing completed and acknowledged", message.ImageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image message");
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel?.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
