namespace PixPro.Services.Notifications.Application.DTOs.Responses;

public record NotificationListResponse(
    List<NotificationResponse> Notifications,
    int TotalCount,
    int Page,
    int PageSize
);
