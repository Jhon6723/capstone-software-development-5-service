using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Application.Commands.MarkAllAsRead;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Result>
{
    private readonly INotificationRepository _repository;

    public MarkAllAsReadCommandHandler(INotificationRepository repository)
    {
        _repository = repository;
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

            // Batch update in write database
            var success = await _repository.MarkAllAsReadAsync(command.UserId);

            if (!success)
                return Result.Failure("No notifications found or all already read");

            // TODO: Publish events for read database sync (US-61 AC #3)
            // This will be implemented when we add event publishing infrastructure
            // For bulk operations, we might publish a single BulkNotificationsReadEvent
            // or individual events for each notification

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error marking all notifications as read: {ex.Message}");
        }
    }
}
