using System.Text.Json;
using Microsoft.Extensions.Logging;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Services;
using StackExchange.Redis;

namespace PixPro.Services.Notifications.Infrastructure.Persistence;

/// <summary>
/// Redis implementation of read repository for CQRS pattern
/// Stores denormalized, query-optimized notification data
/// </summary>
public class RedisNotificationReadRepository : INotificationReadRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisNotificationReadRepository> _logger;
    private readonly IDatabase _database;

    // Redis key patterns
    private const string NotificationKeyPrefix = "notification:";
    private const string UserNotificationsKeyPrefix = "user:notifications:";
    private const string UnreadCountKeyPrefix = "user:unread:";

    public RedisNotificationReadRepository(
        IConnectionMultiplexer redis,
        ILogger<RedisNotificationReadRepository> logger)
    {
        _redis = redis;
        _logger = logger;
        _database = _redis.GetDatabase();
    }

    public async Task<NotificationResponse?> GetByIdAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{NotificationKeyPrefix}{notificationId}";
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Notification {NotificationId} not found in read database", notificationId);
                return null;
            }

            var notification = JsonSerializer.Deserialize<NotificationResponse>(value!);
            _logger.LogDebug("Retrieved notification {NotificationId} from read database", notificationId);
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification {NotificationId} from read database", notificationId);
            throw;
        }
    }

    public async Task<NotificationListResponse> GetUserNotificationsAsync(
        string userId, 
        int pageSize, 
        int page, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{UserNotificationsKeyPrefix}{userId}";
            
            // Get notifications sorted by CreatedAt (score = timestamp)
            var start = (page - 1) * pageSize;
            var stop = start + pageSize - 1;
            
            var notificationIds = await _database.SortedSetRangeByScoreAsync(
                key, 
                order: Order.Descending,
                skip: start,
                take: pageSize);

            var notifications = new List<NotificationResponse>();
            
            foreach (var notificationId in notificationIds)
            {
                var notification = await GetByIdAsync(notificationId!, cancellationToken);
                if (notification != null)
                {
                    notifications.Add(notification);
                }
            }

            var totalCount = await _database.SortedSetLengthAsync(key);

            _logger.LogDebug(
                "Retrieved {Count} notifications for user {UserId} (page {Page}, pageSize {PageSize})",
                notifications.Count, userId, page, pageSize);

            return new NotificationListResponse(
                Notifications: notifications,
                TotalCount: (int)totalCount,
                Page: page,
                PageSize: pageSize
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{UnreadCountKeyPrefix}{userId}";
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Unread count for user {UserId} not found, returning 0", userId);
                return 0;
            }

            var count = (int)value;
            _logger.LogDebug("Retrieved unread count {Count} for user {UserId}", count, userId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread count for user {UserId}", userId);
            throw;
        }
    }

    public async Task SaveNotificationAsync(NotificationResponse notification, CancellationToken cancellationToken = default)
    {
        try
        {
            // Store notification by ID
            var notificationKey = $"{NotificationKeyPrefix}{notification.Id}";
            var notificationJson = JsonSerializer.Serialize(notification);
            await _database.StringSetAsync(notificationKey, notificationJson, TimeSpan.FromDays(30));

            // Add to user's sorted set (score = timestamp for ordering)
            var userKey = $"{UserNotificationsKeyPrefix}{notification.UserId}";
            var score = new DateTimeOffset(notification.CreatedAt).ToUnixTimeSeconds();
            await _database.SortedSetAddAsync(userKey, notification.Id, score);

            // Update unread count if notification is unread
            if (!notification.IsRead)
            {
                await IncrementUnreadCountAsync(notification.UserId);
            }

            _logger.LogInformation(
                "Saved notification {NotificationId} to read database for user {UserId}",
                notification.Id, notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving notification {NotificationId} to read database", notification.Id);
            throw;
        }
    }

    public async Task MarkAsReadAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await GetByIdAsync(notificationId, cancellationToken);
            
            if (notification != null && !notification.IsRead)
            {
                // Update notification
                notification = notification with { IsRead = true };
                await SaveNotificationAsync(notification, cancellationToken);

                // Decrement unread count
                await DecrementUnreadCountAsync(userId);

                _logger.LogInformation(
                    "Marked notification {NotificationId} as read in read database",
                    notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            throw;
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userKey = $"{UserNotificationsKeyPrefix}{userId}";
            var notificationIds = await _database.SortedSetRangeByScoreAsync(userKey);

            foreach (var notificationId in notificationIds)
            {
                await MarkAsReadAsync(notificationId!, userId, cancellationToken);
            }

            // Reset unread count to 0
            var unreadKey = $"{UnreadCountKeyPrefix}{userId}";
            await _database.StringSetAsync(unreadKey, 0);

            _logger.LogInformation("Marked all notifications as read for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await GetByIdAsync(notificationId, cancellationToken);
            
            if (notification != null)
            {
                // Delete notification
                var notificationKey = $"{NotificationKeyPrefix}{notificationId}";
                await _database.KeyDeleteAsync(notificationKey);

                // Remove from user's sorted set
                var userKey = $"{UserNotificationsKeyPrefix}{userId}";
                await _database.SortedSetRemoveAsync(userKey, notificationId);

                // Decrement unread count if it was unread
                if (!notification.IsRead)
                {
                    await DecrementUnreadCountAsync(userId);
                }

                _logger.LogInformation("Deleted notification {NotificationId} from read database", notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            throw;
        }
    }

    private async Task IncrementUnreadCountAsync(string userId)
    {
        var key = $"{UnreadCountKeyPrefix}{userId}";
        await _database.StringIncrementAsync(key);
    }

    private async Task DecrementUnreadCountAsync(string userId)
    {
        var key = $"{UnreadCountKeyPrefix}{userId}";
        var value = await _database.StringDecrementAsync(key);
        
        // Ensure count doesn't go below 0
        if (value < 0)
        {
            await _database.StringSetAsync(key, 0);
        }
    }
}
