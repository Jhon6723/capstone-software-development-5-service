using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace API.Models;

/// <summary>
/// Notification types
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// In-app notification
    /// </summary>
    InApp = 0,

    /// <summary>
    /// Email notification
    /// </summary>
    Email = 1,

    /// <summary>
    /// Push notification
    /// </summary>
    Push = 2,

    /// <summary>
    /// Image-related notification
    /// </summary>
    Image = 3
}

/// <summary>
/// Request model for creating a notification
/// </summary>
public class CreateNotificationRequest
{
    /// <summary>
    /// Target user ID
    /// </summary>
    [Required]
    [DefaultValue("d38ad444-b3d1-458c-bc08-f94bc7c84b78")]
    public required string UserId { get; set; }

    /// <summary>
    /// Notification type
    /// </summary>
    [DefaultValue(NotificationType.InApp)]
    public NotificationType Type { get; set; }

    /// <summary>
    /// Notification title
    /// </summary>
    [Required]
    [DefaultValue("Welcome to PixPro")]
    public required string Title { get; set; }

    /// <summary>
    /// Notification message
    /// </summary>
    [Required]
    [DefaultValue("This is your first notification!")]
    public required string Message { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Notification information
/// </summary>
public class NotificationResponse
{
    /// <summary>
    /// Notification unique identifier
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Target user ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Notification type
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Notification title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Notification message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Whether the notification has been read
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Notification creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Paginated list of notifications
/// </summary>
public class NotificationListResponse
{
    /// <summary>
    /// List of notifications
    /// </summary>
    public required List<NotificationResponse> Notifications { get; set; }

    /// <summary>
    /// Total count of notifications
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
}

/// <summary>
/// Unread notification count
/// </summary>
public class UnreadCountResponse
{
    /// <summary>
    /// Number of unread notifications
    /// </summary>
    public int Count { get; set; }
}
