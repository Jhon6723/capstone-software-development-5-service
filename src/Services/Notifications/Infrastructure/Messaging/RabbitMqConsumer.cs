using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PixPro.Services.Notifications.Application.Commands.CreateNotification;
using PixPro.Services.Notifications.Application.DTOs.Requests;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Enums;
using PixPro.Services.Notifications.Domain.Events;
using PixPro.Services.Notifications.Infrastructure.Messaging.Events;
using PixPro.Services.Notifications.Infrastructure.WebSockets;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PixPro.Services.Notifications.Infrastructure.Messaging;

public class RabbitMqConsumer : BackgroundService
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumer(
        ILogger<RabbitMqConsumer> logger,
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

            ConsumeQueue("user-events", HandleUserEvent, stoppingToken);
            ConsumeQueue("project-events", HandleProjectEvent, stoppingToken);
            ConsumeQueue("notifications", HandleNotificationEvent, stoppingToken);
            ConsumeQueue("image-processing-events", HandleImageProcessingEvent, stoppingToken);
            ConsumeQueue("processed-image-events", HandleImageProcessingEvent, stoppingToken);
            ConsumeQueue("notification-domain-events", HandleDomainEvent, stoppingToken);

            _logger.LogInformation("RabbitMQ Consumer started successfully");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RabbitMQ Consumer");
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

        // Declare exchanges
        _channel.ExchangeDeclare("image-events", ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("processed-image-events", ExchangeType.Fanout, durable: true);

        // Declare and bind queues
        _channel.QueueDeclare(queue: "user-events", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: "project-events", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: "image-processing-events", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: "processed-image-events", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: "notifications", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: "notification-domain-events", durable: true, exclusive: false, autoDelete: false);

        // Bind queues to exchanges
        _channel.QueueBind("image-processing-events", "image-events", "");
        _channel.QueueBind("processed-image-events", "processed-image-events", "");

        _logger.LogInformation("RabbitMQ connection established");
    }

    private void ConsumeQueue(string queueName, Func<string, Task> messageHandler, CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation($"Received message from {queueName}: {message}");

                await messageHandler(message);

                _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message from {queueName}");
                _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel?.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    private async Task HandleUserEvent(string message)
    {
        var userEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(message);

        if (userEvent == null) return;

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CreateNotificationCommand(
            UserId: userEvent.UserId,
            Type: NotificationType.InApp,
            Title: "Welcome to PixPro!",
            Message: $"Hi {userEvent.Name}, welcome to PixPro! We're excited to have you on board.",
            Metadata: new Dictionary<string, string>
            {
                { "eventType", "UserRegistered" },
                { "email", userEvent.Email }
            }
        );

        await mediator.Send(command);
    }

    private async Task HandleProjectEvent(string message)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;

            if (root.TryGetProperty("CreatorId", out _))
            {
                var projectEvent = JsonSerializer.Deserialize<ProjectCreatedEvent>(message);
                if (projectEvent != null)
                    await HandleProjectCreated(projectEvent);
            }
            else if (root.TryGetProperty("UpdatedBy", out _))
            {
                var projectEvent = JsonSerializer.Deserialize<ProjectUpdatedEvent>(message);
                if (projectEvent != null)
                    await HandleProjectUpdated(projectEvent);
            }
            else if (root.TryGetProperty("AssignedUserId", out _))
            {
                var projectEvent = JsonSerializer.Deserialize<ProjectAssignedEvent>(message);
                if (projectEvent != null)
                    await HandleProjectAssigned(projectEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling project event");
        }
    }

    private async Task HandleProjectCreated(ProjectCreatedEvent projectEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        foreach (var userId in projectEvent.TeamMemberIds)
        {
            var command = new CreateNotificationCommand(
                UserId: userId,
                Type: NotificationType.InApp,
                Title: "New Project Created",
                Message: $"You've been added to project: {projectEvent.ProjectName}",
                Metadata: new Dictionary<string, string>
                {
                    { "eventType", "ProjectCreated" },
                    { "projectId", projectEvent.ProjectId }
                }
            );

            await mediator.Send(command);
        }
    }

    private async Task HandleProjectUpdated(ProjectUpdatedEvent projectEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        foreach (var userId in projectEvent.StakeholderIds)
        {
            var command = new CreateNotificationCommand(
                UserId: userId,
                Type: NotificationType.InApp,
                Title: "Project Updated",
                Message: $"Project {projectEvent.ProjectName} has been updated",
                Metadata: new Dictionary<string, string>
                {
                    { "eventType", "ProjectUpdated" },
                    { "projectId", projectEvent.ProjectId }
                }
            );

            await mediator.Send(command);
        }
    }

    private async Task HandleProjectAssigned(ProjectAssignedEvent projectEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CreateNotificationCommand(
            UserId: projectEvent.AssignedUserId,
            Type: NotificationType.InApp,
            Title: "New Project Assignment",
            Message: $"You've been assigned to project: {projectEvent.ProjectName}",
            Metadata: new Dictionary<string, string>
            {
                { "eventType", "ProjectAssigned" },
                { "projectId", projectEvent.ProjectId }
            }
        );

        await mediator.Send(command);
    }

    private async Task HandleNotificationEvent(string message)
    {
        var notificationEvent = JsonSerializer.Deserialize<NotificationEvent>(message);

        if (notificationEvent == null) return;

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CreateNotificationCommand(
            UserId: notificationEvent.UserId,
            Type: notificationEvent.Type,
            Title: notificationEvent.Title,
            Message: notificationEvent.Message,
            Metadata: notificationEvent.Metadata
        );

        await mediator.Send(command);
    }

    private async Task HandleImageProcessingEvent(string message)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;

            if (root.TryGetProperty("ProcessedImageUrls", out _))
            {
                var imageId = root.GetProperty("ImageId").GetString() ?? "";
                var userId = root.GetProperty("UserId").GetString() ?? "";
                var projectId = root.GetProperty("ProjectId").GetString() ?? "";
                var imageUrl = root.GetProperty("ImageUrl").GetString() ?? "";
                var processedImageUrls = root.GetProperty("ProcessedImageUrls").EnumerateArray()
                    .Select(x => x.GetString() ?? "")
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();
                var completedAt = root.GetProperty("CompletedAt").GetDateTime();

                // Extract ProcessingResults as JsonElement (can contain any JSON structure)
                JsonElement? processingResults = null;
                if (root.TryGetProperty("ProcessingResults", out var resultsElement))
                {
                    processingResults = resultsElement;
                }

                var completedEvent = new ImageProcessingCompletedEvent(
                    imageId,
                    userId,
                    projectId,
                    imageUrl,
                    processedImageUrls,
                    processingResults,
                    completedAt
                );

                await HandleImageProcessingCompleted(completedEvent);
            }
            else if (root.TryGetProperty("ErrorMessage", out _))
            {
                var failedEvent = JsonSerializer.Deserialize<ImageProcessingFailedEvent>(message);
                if (failedEvent != null)
                    await HandleImageProcessingFailed(failedEvent);
                else
                    _logger.LogWarning("Failed to deserialize ImageProcessingFailedEvent");
            }
            else if (root.TryGetProperty("OwnerId", out _))
            {
                var uploadedEvent = JsonSerializer.Deserialize<ImageUploadedEvent>(message);
                if (uploadedEvent != null)
                    await HandleImageUploaded(uploadedEvent);
                else
                    _logger.LogWarning("Failed to deserialize ImageUploadedEvent");
            }
            else
            {
                _logger.LogWarning("Unknown image processing event type");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling image processing event");
        }
    }

    private async Task HandleImageUploaded(ImageUploadedEvent imageEvent)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new CreateNotificationCommand(
                UserId: imageEvent.OwnerId,
                Type: NotificationType.Image,
                Title: "Image Uploaded",
                Message: "Your image has been uploaded successfully and is ready for processing.",
                Metadata: new Dictionary<string, string>
                {
                    { "eventType", "ImageUploaded" },
                    { "imageId", imageEvent.ImageId },
                    { "projectId", imageEvent.ProjectId },
                }
            );

            await mediator.Send(command);

            _logger.LogInformation("ImageUploaded notification sent for ImageId: {ImageId}", imageEvent.ImageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HandleImageUploaded for ImageId: {ImageId}", imageEvent.ImageId);
        }
    }

    private async Task HandleImageProcessingCompleted(ImageProcessingCompletedEvent imageEvent)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var metadata = new Dictionary<string, string>
            {
                { "eventType", "ImageProcessingCompleted" },
                { "imageId", imageEvent.ImageId },
                { "imageUrl", imageEvent.ImageUrl },
                { "processedImageUrls", string.Join(",", imageEvent.ProcessedImageUrls) },
                { "projectId", imageEvent.ProjectId },
                { "completedAt", imageEvent.CompletedAt.ToString("O") }
            };

            // Serialize processing results as JSON string (supports arrays, objects, etc.)
            if (imageEvent.ProcessingResults != null)
            {
                var processingResultsJson = imageEvent.ProcessingResults.Value.GetRawText();
                metadata["processingResults"] = processingResultsJson;
            }

            var command = new CreateNotificationCommand(
                UserId: imageEvent.UserId,
                Type: NotificationType.Image,
                Title: "Image Processing Completed",
                Message: "Your image has been processed successfully!",
                Metadata: metadata
            );

            var notificationResult = await mediator.Send(command);

            if (notificationResult.IsSuccess)
            {
                _logger.LogInformation($"Image processing completed notification sent to user {imageEvent.UserId}");
            }
            else
            {
                _logger.LogError($"Failed to create notification. Error: {notificationResult.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in HandleImageProcessingCompleted for user {imageEvent.UserId}");
        }
    }

    private async Task HandleImageProcessingFailed(ImageProcessingFailedEvent imageEvent)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var metadata = new Dictionary<string, string>
            {
                { "eventType", "ImageProcessingFailed" },
                { "imageId", imageEvent.ImageId },
                { "imageUrl", imageEvent.ImageUrl },
                { "errorMessage", imageEvent.ErrorMessage },
                { "errorCode", imageEvent.ErrorCode },
                { "projectId", imageEvent.ProjectId },
                { "failedAt", imageEvent.FailedAt.ToString("O") }
            };

            var command = new CreateNotificationCommand(
                UserId: imageEvent.UserId,
                Type: NotificationType.Image,
                Title: "Image Processing Failed",
                Message: $"Failed to process your image: {imageEvent.ErrorMessage}",
                Metadata: metadata
            );

            var notificationResult = await mediator.Send(command);

            if (notificationResult.IsSuccess)
            {
                _logger.LogInformation($"Image processing failed notification sent to user {imageEvent.UserId}");
            }
            else
            {
                _logger.LogError($"Failed to create notification. Error: {notificationResult.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in HandleImageProcessingFailed for user {imageEvent.UserId}");
        }
    }

    private async Task HandleDomainEvent(string message)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;

            string? eventType = null;

            if (root.TryGetProperty("notificationId", out _) && root.TryGetProperty("title", out _))
            {
                eventType = nameof(NotificationCreatedEvent);
            }
            else if (root.TryGetProperty("notificationId", out _) && root.TryGetProperty("userId", out _) && !root.TryGetProperty("title", out _) && !root.TryGetProperty("notificationsCount", out _))
            {
                eventType = nameof(NotificationReadEvent);
            }
            else if (root.TryGetProperty("notificationsCount", out _))
            {
                eventType = nameof(NotificationBatchReadEvent);
            }

            using var scope = _serviceProvider.CreateScope();
            var readRepository = scope.ServiceProvider.GetRequiredService<INotificationReadRepository>();

            switch (eventType)
            {
                case nameof(NotificationCreatedEvent):
                    await HandleNotificationCreatedEvent(message, readRepository);
                    break;

                case nameof(NotificationReadEvent):
                    if (message.Contains("\"eventId\"") && message.Contains("\"notificationId\"") && message.Contains("\"userId\""))
                    {
                        try
                        {
                            var testEvent = JsonSerializer.Deserialize<NotificationDeletedEvent>(message, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            if (testEvent != null)
                            {
                                await HandleNotificationDeletedEvent(message, readRepository);
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    await HandleNotificationReadEvent(message, readRepository);
                    break;

                case nameof(NotificationBatchReadEvent):
                    await HandleNotificationBatchReadEvent(message, readRepository);
                    break;

                default:
                    _logger.LogWarning("Unknown domain event type in message: {Message}", message);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling domain event");
            throw;
        }
    }

    private async Task HandleNotificationCreatedEvent(string message, INotificationReadRepository readRepository)
    {
        var @event = JsonSerializer.Deserialize<NotificationCreatedEvent>(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (@event == null)
        {
            _logger.LogError("Failed to deserialize NotificationCreatedEvent");
            return;
        }

        var notification = new NotificationResponse(
            Id: @event.NotificationId,
            UserId: @event.UserId,
            Type: @event.Type,
            Title: @event.Title,
            Message: @event.Message,
            IsRead: false,
            CreatedAt: @event.OccurredAt,
            Metadata: @event.Metadata
        );

        await readRepository.SaveNotificationAsync(notification);

        _logger.LogInformation(
            "Synced NotificationCreatedEvent to Redis read database: {NotificationId}",
            @event.NotificationId);

        // Send notification to WebSocket clients
        using var scope = _serviceProvider.CreateScope();
        var webSocketService = scope.ServiceProvider.GetRequiredService<IWebSocketNotificationService>();

        // Determine notification type based on metadata
        string notificationType = "NOTIFICATION";
        if (@event.Metadata != null && @event.Metadata.TryGetValue("eventType", out var eventType))
        {
            notificationType = eventType switch
            {
                "ImageUploaded" => "IMAGE_UPLOADED",
                "ImageProcessingCompleted" => "IMAGE_PROCESSING_COMPLETED",
                "ImageProcessingFailed" => "IMAGE_PROCESSING_FAILED",
                _ => "NOTIFICATION"
            };
        }

        // Build error details for failed events
        object? errorDetails = null;
        if (notificationType == "IMAGE_PROCESSING_FAILED" && @event.Metadata != null)
        {
            @event.Metadata.TryGetValue("errorMessage", out var errorMessage);
            @event.Metadata.TryGetValue("errorCode", out var errorCode);
            if (errorMessage != null || errorCode != null)
            {
                errorDetails = new { message = errorMessage, code = errorCode };
            }
        }

        var wsNotification = new
        {
            type = notificationType,
            notification = notification,
            error = errorDetails,
            timestamp = DateTime.UtcNow
        };

        await webSocketService.SendNotificationAsync(@event.UserId, wsNotification);

        _logger.LogInformation(
            "Sent notification {NotificationId} to WebSocket clients for user {UserId}",
            @event.NotificationId,
            @event.UserId);
    }

    private async Task HandleNotificationReadEvent(string message, INotificationReadRepository readRepository)
    {
        var @event = JsonSerializer.Deserialize<NotificationReadEvent>(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (@event == null)
        {
            _logger.LogError("Failed to deserialize NotificationReadEvent");
            return;
        }

        await readRepository.MarkAsReadAsync(@event.NotificationId, @event.UserId);

        _logger.LogInformation(
            "Synced NotificationReadEvent to Redis read database: {NotificationId}",
            @event.NotificationId);
    }

    private async Task HandleNotificationBatchReadEvent(string message, INotificationReadRepository readRepository)
    {
        var @event = JsonSerializer.Deserialize<NotificationBatchReadEvent>(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (@event == null)
        {
            _logger.LogError("Failed to deserialize NotificationBatchReadEvent");
            return;
        }

        await readRepository.MarkAllAsReadAsync(@event.UserId);

        _logger.LogInformation(
            "Synced NotificationBatchReadEvent to Redis read database for user: {UserId}",
            @event.UserId);
    }

    private async Task HandleNotificationDeletedEvent(string message, INotificationReadRepository readRepository)
    {
        var @event = JsonSerializer.Deserialize<NotificationDeletedEvent>(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (@event == null)
        {
            _logger.LogError("Failed to deserialize NotificationDeletedEvent");
            return;
        }

        await readRepository.DeleteAsync(@event.NotificationId, @event.UserId);

        _logger.LogInformation(
            "Synced NotificationDeletedEvent to Redis read database: {NotificationId}",
            @event.NotificationId);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
