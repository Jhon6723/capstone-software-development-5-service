using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Entities;
using PixPro.Services.Notifications.Domain.Events;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Application.Commands.CreateNotification;

public class CreateNotificationCommandHandler 
    : IRequestHandler<CreateNotificationCommand, Result<NotificationResponse>>
{
    private readonly INotificationRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public CreateNotificationCommandHandler(
        INotificationRepository repository,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<NotificationResponse>> Handle(
        CreateNotificationCommand command, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate command
            if (string.IsNullOrWhiteSpace(command.UserId))
                return Result<NotificationResponse>.Failure("UserId is required");

            if (string.IsNullOrWhiteSpace(command.Title))
                return Result<NotificationResponse>.Failure("Title is required");

            if (string.IsNullOrWhiteSpace(command.Message))
                return Result<NotificationResponse>.Failure("Message is required");

            // Create notification entity
            var notification = new Notification
            {
                UserId = command.UserId,
                Type = command.Type,
                Title = command.Title,
                Message = command.Message,
                Metadata = command.Metadata
            };

            // Persist to write database
            var createdNotification = await _repository.CreateAsync(notification);

            // Publish NotificationCreatedEvent to message broker for read DB sync
            var domainEvent = new NotificationCreatedEvent
            {
                NotificationId = createdNotification.Id,
                UserId = createdNotification.UserId,
                Type = createdNotification.Type,
                Title = createdNotification.Title,
                Message = createdNotification.Message,
                Metadata = createdNotification.Metadata
            };
            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

            // Map to response (without querying full entity again)
            var response = new NotificationResponse(
                Id: createdNotification.Id,
                UserId: createdNotification.UserId,
                Type: createdNotification.Type,
                Title: createdNotification.Title,
                Message: createdNotification.Message,
                IsRead: createdNotification.IsRead,
                CreatedAt: createdNotification.CreatedAt,
                Metadata: createdNotification.Metadata
            );

            return Result<NotificationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<NotificationResponse>.Failure($"Error creating notification: {ex.Message}");
        }
    }
}
