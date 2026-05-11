using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;

namespace PixPro.Services.Notifications.Application.Queries.GetUnreadCount;

public record GetUnreadCountQuery(
    string UserId
) : IRequest<Result<int>>;
