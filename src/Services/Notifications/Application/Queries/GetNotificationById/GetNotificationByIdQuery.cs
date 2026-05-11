using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;

namespace PixPro.Services.Notifications.Application.Queries.GetNotificationById;

public record GetNotificationByIdQuery(
    string NotificationId
) : IRequest<Result<NotificationResponse>>;
