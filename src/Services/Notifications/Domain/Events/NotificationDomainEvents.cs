using PixPro.Services.Notifications.Domain.Enums;

namespace PixPro.Services.Notifications.Domain.Events;

/// <summary>
/// Base event for all notification domain events
/// </summary>
public abstract record NotificationDomainEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a new notification is created
/// </summary>
public record NotificationCreatedEvent : NotificationDomainEvent
{
    public string NotificationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event published when a notification is marked as read
/// </summary>
public record NotificationReadEvent : NotificationDomainEvent
{
    public string NotificationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}

/// <summary>
/// Event published when all notifications for a user are marked as read
/// </summary>
public record NotificationBatchReadEvent : NotificationDomainEvent
{
    public string UserId { get; init; } = string.Empty;
    public int NotificationsCount { get; init; }
}
