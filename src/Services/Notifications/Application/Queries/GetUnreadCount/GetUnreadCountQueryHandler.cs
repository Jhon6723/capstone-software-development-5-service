using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Application.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
{
    private readonly INotificationRepository _repository;

    public GetUnreadCountQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(
        GetUnreadCountQuery query, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate query
            if (string.IsNullOrWhiteSpace(query.UserId))
                return Result<int>.Failure("UserId is required");

            // TODO: Read from cached/optimized read database - US-65/US-66
            // For now, reading from write database (MongoDB)
            // This should be one of the first queries to cache (US-66 AC #1: Cache unread counts per user - 5 min TTL)
            // Target: Returns count in under 100ms (US-63 AC #3)
            var count = await _repository.GetUnreadCountAsync(query.UserId);

            // TODO: Updates in real-time via WebSocket - US-63 AC #4
            // WebSocket integration will push updates when notifications are created/read
            // This requires WebSocket infrastructure already present in the service

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Error retrieving unread count: {ex.Message}");
        }
    }
}
