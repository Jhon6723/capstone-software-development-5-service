namespace PixPro.Services.Projects.Application.IntegrationEvents;

// DTO for deserializing the event from IA service
public class ImageProcessingCompletedEvent
{
    public string ImageId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<string> ProcessedImageUrls { get; set; } = new();
}
