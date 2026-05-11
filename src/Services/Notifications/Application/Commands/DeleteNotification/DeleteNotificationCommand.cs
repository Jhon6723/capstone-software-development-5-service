using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;

namespace PixPro.Services.Notifications.Application.Commands.DeleteNotification;

public record DeleteNotificationCommand(
    string NotificationId
) : IRequest<Result>;
