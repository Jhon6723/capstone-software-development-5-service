using PixPro.Services.Projects.Domain.Entities;

namespace PixPro.Services.Projects.Domain.Repositories;

public interface IImageRepository
{
    Task AddAsync(Image image, CancellationToken cancellationToken = default);
    Task<Image?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<int> CountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<(List<Image> Images, int Total)> GetPaginatedByProjectIdAsync(
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
