using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Services;

namespace PixPro.Services.Notifications.Application.Queries.GetUnreadNotifications;

public class GetUnreadNotificationsQueryHandler 
    : IRequestHandler<GetUnreadNotificationsQuery, Result<List<NotificationResponse>>>
{
    private readonly INotificationReadRepository _readRepository;

    public GetUnreadNotificationsQueryHandler(INotificationReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<List<NotificationResponse>>> Handle(
        GetUnreadNotificationsQuery request, 
        CancellationToken cancellationToken)
    {
        // Query from Redis read database
        var notifications = await _readRepository.GetUnreadByUserIdAsync(request.UserId, cancellationToken);

        return Result<List<NotificationResponse>>.Success(notifications);
    }
}
