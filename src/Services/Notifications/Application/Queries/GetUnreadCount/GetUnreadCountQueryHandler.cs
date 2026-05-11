using MediatR;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.Services;

namespace PixPro.Services.Notifications.Application.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
{
    private readonly INotificationReadRepository _readRepository;

    public GetUnreadCountQueryHandler(INotificationReadRepository readRepository)
    {
        _readRepository = readRepository;
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

            // Read from optimized read database (Redis)
            // This query is cached and highly performant
            var count = await _readRepository.GetUnreadCountAsync(query.UserId, cancellationToken);

            // TODO: Cache with 5 min TTL - US-66 AC #1
            // TODO: Updates in real-time via WebSocket - US-63 AC #4
            // Target: Returns count in under 100ms (US-63 AC #3)

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Error retrieving unread count: {ex.Message}");
        }
    }
}
