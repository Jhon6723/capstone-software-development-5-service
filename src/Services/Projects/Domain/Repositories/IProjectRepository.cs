using PixPro.Services.Projects.Domain.Entities;

namespace PixPro.Services.Projects.Domain.Repositories;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
