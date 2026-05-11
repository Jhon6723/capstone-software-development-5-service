using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;

namespace PixPro.Services.Notifications.Application.Queries.GetUnreadNotifications;

public record GetUnreadNotificationsQuery(
    string UserId
) : IRequest<Result<List<NotificationResponse>>>;
