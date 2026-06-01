using System.Text.Json.Serialization;

namespace PixPro.Services.Projects.Application.IntegrationEvents;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProcessingFeature
{
    Generator = 0,  // Text-to-image generation
    Editor = 1      // Image-to-image editing
}

public record ProcessingParameters(
    int Width = 512,
    int Height = 512,
    int NumInferenceSteps = 20,
    double Strength = 0.75,
    double GuidanceScale = 7.5,
    int Quantity = 1,
    string Model = "gpt-image-1-mini-low"
);

public record ImageUploadedEvent(
    Guid ImageId,
    Guid OwnerId,
    Guid ProjectId,
    string? ImageUrl,
    string Prompt,
    ProcessingFeature Feature,
    ProcessingParameters? Parameters = null
);
