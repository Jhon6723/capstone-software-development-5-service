using PixPro.Services.Projects.Application.Common.Results;
using PixPro.Services.Projects.Application.DTOs.Requests;
using PixPro.Services.Projects.Application.DTOs.Responses;

namespace PixPro.Services.Projects.Application.Services;

public interface IProjectService
{
    Task<Result<ProjectListResponse>> GetProjectsAsync(Guid ownerId, string? search = null, CancellationToken cancellationToken = default);
    Task<Result<ProjectResponse?>> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Result<ProjectResponse>> CreateProjectAsync(Guid ownerId, CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<Result<ImageListResponse>> GetProjectImagesAsync(Guid projectId, GetProjectImagesRequest request, CancellationToken cancellationToken = default);
}
