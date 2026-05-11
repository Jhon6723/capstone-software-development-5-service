using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;

namespace PixPro.Services.Notifications.Application.Commands.MarkAsRead;

public record MarkAsReadCommand(
    string NotificationId
) : IRequest<Result>;
