using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;

namespace PixPro.Services.Notifications.Application.Queries.GetUserNotifications;

public record GetUserNotificationsQuery(
    string UserId,
    int PageSize = 20,
    int Page = 1
) : IRequest<Result<NotificationListResponse>>;
