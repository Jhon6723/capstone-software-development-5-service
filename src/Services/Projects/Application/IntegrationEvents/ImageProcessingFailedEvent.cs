namespace PixPro.Services.Projects.Application.IntegrationEvents;

public class ImageProcessingFailedEvent
{
    public string ImageId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public DateTimeOffset FailedAt { get; set; }
    public string? ModelUsed { get; set; }
}
