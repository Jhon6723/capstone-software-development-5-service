using PixPro.Services.Projects.Domain.Entities;

namespace PixPro.Services.Projects.Domain.Repositories;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Project> Projects, int Total)> GetPaginatedByOwnerIdAsync(
        Guid ownerId,
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
