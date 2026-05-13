using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Events;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Application.Commands.MarkAllAsRead;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public MarkAllAsReadCommandHandler(
        INotificationRepository repository,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(
        MarkAllAsReadCommand command, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate command
            if (string.IsNullOrWhiteSpace(command.UserId))
                return Result.Failure("UserId is required");

            // Get unread count before marking as read
            var unreadCount = await _repository.GetUnreadCountAsync(command.UserId);

            // Batch update in write database
            var success = await _repository.MarkAllAsReadAsync(command.UserId);

            if (!success)
                return Result.Failure("No notifications found or all already read");

            // Publish batch event for read database sync
            var domainEvent = new NotificationBatchReadEvent
            {
                UserId = command.UserId,
                NotificationsCount = unreadCount
            };
            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error marking all notifications as read: {ex.Message}");
        }
    }
}
