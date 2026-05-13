using PixPro.Services.Projects.Domain.Entities;

namespace PixPro.Services.Projects.Domain.Repositories;

public interface IImageRepository
{
    Task AddAsync(Image image, CancellationToken cancellationToken = default);
    Task<Image?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
