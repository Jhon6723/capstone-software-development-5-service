using PixPro.Services.Notifications.Application.DTOs.Responses;

namespace PixPro.Services.Notifications.Application.Services;

/// <summary>
/// Repository for reading notifications from the optimized read database (Redis)
/// Part of CQRS pattern - handles all read operations
/// </summary>
public interface INotificationReadRepository
{
    /// <summary>
    /// Get a notification by ID from read database
    /// </summary>
    Task<NotificationResponse?> GetByIdAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated notifications for a user from read database
    /// </summary>
    Task<NotificationListResponse> GetUserNotificationsAsync(
        string userId, 
        int pageSize, 
        int page, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread notification count for a user from read database
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store/update notification in read database (called by event handlers)
    /// </summary>
    Task SaveNotificationAsync(NotificationResponse notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark notification as read in read database (called by event handlers)
    /// </summary>
    Task MarkAsReadAsync(string notificationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications as read for a user in read database (called by event handlers)
    /// </summary>
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete notification from read database
    /// </summary>
    Task DeleteAsync(string notificationId, string userId, CancellationToken cancellationToken = default);
}
