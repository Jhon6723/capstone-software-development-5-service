using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Services;

namespace PixPro.Services.Notifications.Application.Queries.GetUserNotifications;

public class GetUserNotificationsQueryHandler 
    : IRequestHandler<GetUserNotificationsQuery, Result<NotificationListResponse>>
{
    private readonly INotificationReadRepository _readRepository;

    public GetUserNotificationsQueryHandler(INotificationReadRepository readRepository)
    {
        _readRepository = readRepository;
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

            // Read from dedicated read database (Redis)
            // Optimized for query patterns with pagination support
            var response = await _readRepository.GetUserNotificationsAsync(
                query.UserId, 
                query.PageSize, 
                query.Page,
                cancellationToken
            );

            // TODO: Use cached results when available - US-66
            // Caching layer will be implemented in US-66 (Implement Caching Layer)

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
