namespace PixPro.Services.Notifications.Application.DTOs.Requests;

public record GetNotificationsRequest(
    string UserId,
    int PageSize = 20,
    int Page = 1
);
