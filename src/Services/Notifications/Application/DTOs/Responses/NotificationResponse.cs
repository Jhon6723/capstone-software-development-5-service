using PixPro.Services.Notifications.Domain.Enums;

namespace PixPro.Services.Notifications.Application.DTOs.Responses;

public record NotificationResponse(
    string Id,
    string UserId,
    NotificationType Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    Dictionary<string, object>? Metadata
);
