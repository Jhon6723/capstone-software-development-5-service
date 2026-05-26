using PixPro.Services.Projects.Application.IntegrationEvents;

namespace PixPro.Services.Projects.Application.DTOs.Responses;

public record ImageUploadResponse(
    Guid ImageId,
    string FileName,
    string Url,
    string SecureUrl,
    string Format,
    long SizeInBytes,
    int Width,
    int Height,
    DateTimeOffset UploadedAt,
    ProcessingFeature Feature
);
