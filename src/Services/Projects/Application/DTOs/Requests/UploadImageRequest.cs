using Microsoft.AspNetCore.Http;

namespace PixPro.Services.Projects.Application.DTOs.Requests;

public record UploadImageRequest(IFormFile File, Guid OwnerId);
