using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Domain.Enums;

namespace PixPro.Services.Notifications.Application.Commands.CreateNotification;

public record CreateNotificationCommand(
    string UserId,
    NotificationType Type,
    string Title,
    string Message,
    Dictionary<string, string>? Metadata = null
) : IRequest<Result<NotificationResponse>>;
