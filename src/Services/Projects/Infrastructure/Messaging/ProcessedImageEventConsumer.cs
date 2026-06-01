using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PixPro.Services.Projects.Application.IntegrationEvents;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PixPro.Services.Projects.Infrastructure.Messaging;

public class ProcessedImageEventConsumer : BackgroundService
{
    private readonly ILogger<ProcessedImageEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IModel? _channel;

    public ProcessedImageEventConsumer(
        IConfiguration configuration,
        ILogger<ProcessedImageEventConsumer> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange (same as IA publishes to)
        _channel.ExchangeDeclare("processed-image-events", ExchangeType.Fanout, durable: true);

        _logger.LogInformation("ProcessedImageEventConsumer initialized, will bind to 'processed-image-events'");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Declare durable queue (survives restarts)
        var queueName = "projects-processed-images-queue";
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, "processed-image-events", routingKey: "");

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            try
            {
                var eventData = JsonSerializer.Deserialize<ImageProcessingCompletedEvent>(json);

                if (eventData != null)
                {
                    _logger.LogInformation(
                        "Received processed image event for ImageId: {ImageId}, ProjectId: {ProjectId}",
                        eventData.ImageId,
                        eventData.ProjectId);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var imageRepository = scope.ServiceProvider.GetRequiredService<IImageRepository>();

                            // Save each processed image URL as a new Image entity
                            if (eventData.ProcessedImageUrls != null)
                            {
                                var originalImageId = Guid.Parse(eventData.ImageId);
                                var originalImage = await imageRepository.GetByIdAsync(originalImageId);
                                
                                // If original image exists (Feature 1 - Editor), reference it
                                // If not (Feature 0 - Generator), use null
                                var referenceId = originalImage != null ? originalImageId : (Guid?)null;

                                foreach (var processedUrl in eventData.ProcessedImageUrls)
                                {
                                    var processedImage = Image.CreateProcessedImage(
                                        referenceId,  // Real ID if exists, null for generated images
                                        Guid.Parse(eventData.ProjectId),
                                        Guid.Parse(eventData.UserId),
                                        processedUrl,
                                        "processed");

                                    await imageRepository.AddAsync(processedImage);
                                }

                                await imageRepository.SaveChangesAsync();

                                _logger.LogInformation(
                                    "Saved {Count} processed images for original ImageId: {ImageId}",
                                    eventData.ProcessedImageUrls.Count,
                                    eventData.ImageId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error saving processed images for ImageId: {ImageId}", eventData.ImageId);
                        }
                    }, stoppingToken);
                }

                _channel?.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", json);
                _channel?.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsume(queueName, false, consumer);

        _logger.LogInformation("Started consuming from queue '{QueueName}'", queueName);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
