using Microsoft.AspNetCore.Http;
using PixPro.Services.Projects.Application.IntegrationEvents;

namespace PixPro.Services.Projects.Application.DTOs.Requests;

public record UploadImageRequest(
    IFormFile? File,
    Guid OwnerId,
    Guid ProjectId,
    string Prompt,
    ProcessingFeature? Feature = null,  // Optional: 0=Generator, 1=Editor. Auto-detected if null.
    ProcessingParameters? Parameters = null
);
