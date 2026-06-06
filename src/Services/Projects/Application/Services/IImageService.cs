using PixPro.Services.Projects.Application.Common.Results;
using PixPro.Services.Projects.Application.DTOs.Requests;
using PixPro.Services.Projects.Application.DTOs.Responses;

namespace PixPro.Services.Projects.Application.Services;

public interface IImageService
{
    Task<Result<ImageUploadResponse>> UploadAsync(UploadImageRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteImageAsync(Guid imageId, Guid requestingUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
