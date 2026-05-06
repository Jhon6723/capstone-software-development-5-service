using PixPro.Services.Notifications.Domain.Enums;

namespace PixPro.Services.Notifications.Application.DTOs.Requests;

public record CreateNotificationRequest(
    string UserId,
    NotificationType Type,
    string Title,
    string Message,
    Dictionary<string, string>? Metadata = null
);
