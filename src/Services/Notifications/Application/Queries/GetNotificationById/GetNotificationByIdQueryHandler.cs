using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Application.Queries.GetNotificationById;

public class GetNotificationByIdQueryHandler 
    : IRequestHandler<GetNotificationByIdQuery, Result<NotificationResponse>>
{
    private readonly INotificationRepository _repository;

    public GetNotificationByIdQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<NotificationResponse>> Handle(
        GetNotificationByIdQuery query, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate query
            if (string.IsNullOrWhiteSpace(query.NotificationId))
                return Result<NotificationResponse>.Failure("NotificationId is required");

            // TODO: Read from read database (Redis or MongoDB read replica) - US-65
            // For now, reading from write database (MongoDB)
            var notification = await _repository.GetByIdAsync(query.NotificationId);

            // Returns 404 if not found (US-64 AC #3)
            if (notification == null)
                return Result<NotificationResponse>.Failure("Notification not found");

            // Map to response DTO
            var response = new NotificationResponse(
                Id: notification.Id,
                UserId: notification.UserId,
                Type: notification.Type,
                Title: notification.Title,
                Message: notification.Message,
                IsRead: notification.IsRead,
                CreatedAt: notification.CreatedAt,
                Metadata: notification.Metadata
            );

            return Result<NotificationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<NotificationResponse>.Failure(
                $"Error retrieving notification: {ex.Message}"
            );
        }
    }
}
