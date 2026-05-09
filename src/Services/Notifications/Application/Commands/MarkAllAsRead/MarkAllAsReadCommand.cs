using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;

namespace PixPro.Services.Notifications.Application.Commands.MarkAllAsRead;

public record MarkAllAsReadCommand(
    string UserId
) : IRequest<Result>;
