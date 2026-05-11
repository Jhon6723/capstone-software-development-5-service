using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Application.Commands.MarkAsRead;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result>
{
    private readonly INotificationRepository _repository;

    public MarkAsReadCommandHandler(INotificationRepository repository)
    {
        _repository = repository;
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

            // Update write database
            var success = await _repository.MarkAsReadAsync(command.NotificationId);

            if (!success)
                return Result.Failure("Notification not found or already read");

            // TODO: Publish NotificationReadEvent to sync read database (US-60 AC #3)
            // This will be implemented when we add event publishing infrastructure for read DB sync

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error marking notification as read: {ex.Message}");
        }
    }
}
