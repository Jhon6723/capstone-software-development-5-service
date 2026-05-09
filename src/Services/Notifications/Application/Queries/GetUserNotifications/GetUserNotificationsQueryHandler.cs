using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Application.Queries.GetUserNotifications;

public class GetUserNotificationsQueryHandler 
    : IRequestHandler<GetUserNotificationsQuery, Result<NotificationListResponse>>
{
    private readonly INotificationRepository _repository;

    public GetUserNotificationsQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<NotificationListResponse>> Handle(
        GetUserNotificationsQuery query, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate query
            if (string.IsNullOrWhiteSpace(query.UserId))
                return Result<NotificationListResponse>.Failure("UserId is required");

            if (query.PageSize <= 0)
                return Result<NotificationListResponse>.Failure("PageSize must be greater than 0");

            if (query.Page <= 0)
                return Result<NotificationListResponse>.Failure("Page must be greater than 0");

            // TODO: Read from dedicated read database (Redis or MongoDB read replica) - US-65
            // For now, reading from write database (MongoDB)
            // This will be optimized when we implement US-65 (Separate Read Database)
            var notifications = await _repository.GetByUserIdAsync(
                query.UserId, 
                query.PageSize, 
                query.Page
            );

            // TODO: Use cached results when available - US-66
            // Caching layer will be implemented in US-66 (Implement Caching Layer)

            // Map to response DTOs
            var responses = notifications.Select(n => new NotificationResponse(
                Id: n.Id,
                UserId: n.UserId,
                Type: n.Type,
                Title: n.Title,
                Message: n.Message,
                IsRead: n.IsRead,
                CreatedAt: n.CreatedAt,
                Metadata: n.Metadata
            )).ToList();

            // Get total count for pagination
            var totalCount = await _repository.GetUnreadCountAsync(query.UserId);

            var response = new NotificationListResponse(
                responses,
                totalCount,
                query.Page,
                query.PageSize
            );

            return Result<NotificationListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<NotificationListResponse>.Failure(
                $"Error retrieving user notifications: {ex.Message}"
            );
        }
    }
}
