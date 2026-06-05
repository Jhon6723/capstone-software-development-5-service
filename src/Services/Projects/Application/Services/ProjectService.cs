using PixPro.Services.Projects.Application.Common.Results;
using PixPro.Services.Projects.Application.DTOs.Requests;
using PixPro.Services.Projects.Application.DTOs.Responses;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.Services.Projects.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IImageRepository _imageRepository;

    public ProjectService(IProjectRepository projectRepository, IImageRepository imageRepository)
    {
        _projectRepository = projectRepository;
        _imageRepository = imageRepository;
    }

    public async Task<Result<ProjectListResponse>> GetProjectsAsync(
        Guid ownerId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetByOwnerIdAsync(ownerId, cancellationToken);

        // Filter by search if provided
        if (!string.IsNullOrWhiteSpace(search))
        {
            projects = projects.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var projectResponses = new List<ProjectResponse>();

        foreach (var project in projects)
        {
            var imageCount = await _imageRepository.CountByProjectIdAsync(project.Id, cancellationToken);
            projectResponses.Add(new ProjectResponse
            {
                Id = project.Id.ToString(),
                Name = project.Name,
                CreatedAt = project.CreatedAt,
                ImageCount = imageCount
            });
        }

        var response = new ProjectListResponse
        {
            Data = projectResponses,
            Total = projectResponses.Count,
            Page = 1,
            PageSize = projectResponses.Count,
            HasMore = false
        };

        return Result<ProjectListResponse>.Success(response);
    }

    public async Task<Result<ProjectResponse?>> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

        if (project == null)
            return Result<ProjectResponse?>.Success(null);

        var imageCount = await _imageRepository.CountByProjectIdAsync(project.Id, cancellationToken);
        var response = new ProjectResponse
        {
            Id = project.Id.ToString(),
            Name = project.Name,
            CreatedAt = project.CreatedAt,
            ImageCount = imageCount
        };

        return Result<ProjectResponse?>.Success(response);
    }

    public async Task<Result<ProjectResponse>> CreateProjectAsync(
        Guid ownerId,
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<ProjectResponse>.Failure("Project name is required.");

        var project = new Project(request.Name, ownerId);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        var response = new ProjectResponse
        {
            Id = project.Id.ToString(),
            Name = project.Name,
            CreatedAt = project.CreatedAt,
            ImageCount = 0
        };

        return Result<ProjectResponse>.Success(response);
    }

    public async Task<Result<ImageListResponse>> GetProjectImagesAsync(
        Guid projectId,
        GetProjectImagesRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 12 : request.PageSize > 100 ? 100 : request.PageSize;

        var (images, total) = await _imageRepository.GetPaginatedByProjectIdAsync(
            projectId,
            page,
            pageSize,
            cancellationToken);

        var imageResponses = images.Select(image => new ImageResponse
        {
            Id = image.Id.ToString(),
            ProjectId = image.ProjectId.ToString(),
            FileName = image.FileName,
            ContentType = image.ContentType,
            FilePath = image.FilePath,
            CloudinaryPublicId = image.CloudinaryPublicId,
            SecureUrl = image.SecureUrl,
            Format = image.Format,
            SizeInBytes = image.SizeInBytes,
            Width = image.Width,
            Height = image.Height,
            Status = image.Status,
            OwnerId = image.OwnerId.ToString(),
            CreatedAt = image.CreatedAt
        }).ToList();

        var response = new ImageListResponse
        {
            Data = imageResponses,
            Total = total,
            Page = page,
            PageSize = pageSize,
            HasMore = page * pageSize < total
        };

        return Result<ImageListResponse>.Success(response);
    }
}
