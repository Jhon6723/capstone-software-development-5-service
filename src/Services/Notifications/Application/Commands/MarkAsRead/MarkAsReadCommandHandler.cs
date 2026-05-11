using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Events;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Application.Commands.MarkAsRead;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public MarkAsReadCommandHandler(
        INotificationRepository repository,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(
        MarkAsReadCommand command, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate command
            if (string.IsNullOrWhiteSpace(command.NotificationId))
                return Result.Failure("NotificationId is required");

            // Get notification to retrieve UserId
            var notification = await _repository.GetByIdAsync(command.NotificationId);
            if (notification == null)
                return Result.Failure("Notification not found");

            // Update write database
            var success = await _repository.MarkAsReadAsync(command.NotificationId);

            if (!success)
                return Result.Failure("Notification not found or already read");

            // Publish NotificationReadEvent to sync read database
            var domainEvent = new NotificationReadEvent
            {
                NotificationId = command.NotificationId,
                UserId = notification.UserId
            };
            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error marking notification as read: {ex.Message}");
        }
    }
}
