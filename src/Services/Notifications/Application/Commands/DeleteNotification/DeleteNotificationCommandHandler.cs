using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Repositories;
using PixPro.Services.Notifications.Domain.Events;

namespace PixPro.Services.Notifications.Application.Commands.DeleteNotification;

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public DeleteNotificationCommandHandler(
        INotificationRepository repository,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        // Get notification from write database
        var notification = await _repository.GetByIdAsync(request.NotificationId);
        if (notification == null)
        {
            return Result.Failure("Notification not found");
        }

        // Delete from write database (MongoDB)
        await _repository.DeleteAsync(request.NotificationId);

        // Publish domain event for CQRS synchronization
        var deletedEvent = new NotificationDeletedEvent(
            request.NotificationId,
            notification.UserId,
            DateTime.UtcNow
        );
        await _eventPublisher.PublishAsync(deletedEvent, cancellationToken);

        return Result.Success();
    }
}
