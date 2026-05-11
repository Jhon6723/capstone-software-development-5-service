using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Services;

namespace PixPro.Services.Notifications.Application.Queries.GetNotificationById;

public class GetNotificationByIdQueryHandler 
    : IRequestHandler<GetNotificationByIdQuery, Result<NotificationResponse>>
{
    private readonly INotificationReadRepository _readRepository;

    public GetNotificationByIdQueryHandler(INotificationReadRepository readRepository)
    {
        _readRepository = readRepository;
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

            // Read from read database (Redis)
            var notification = await _readRepository.GetByIdAsync(query.NotificationId, cancellationToken);

            // Returns 404 if not found (US-64 AC #3)
            if (notification == null)
                return Result<NotificationResponse>.Failure("Notification not found");

            return Result<NotificationResponse>.Success(notification);
        }
        catch (Exception ex)
        {
            return Result<NotificationResponse>.Failure(
                $"Error retrieving notification: {ex.Message}"
            );
        }
    }
}
