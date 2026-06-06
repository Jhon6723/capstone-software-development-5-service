using Microsoft.EntityFrameworkCore;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.Services.Projects.Infrastructure.Persistence.Repositories;

public sealed class ImageRepository(ProjectsDbContext context) : IImageRepository
{
    private readonly ProjectsDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task AddAsync(Image image, CancellationToken cancellationToken = default)
    {
        await _context.Images.AddAsync(image, cancellationToken);
    }

    public async Task<Image?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Images.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<int> CountByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Images.CountAsync(i => i.OwnerId == ownerId, cancellationToken);
    }

    public async Task<int> CountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Images.CountAsync(i => i.ProjectId == projectId, cancellationToken);
    }

    public async Task<(List<Image> Images, int Total)> GetPaginatedByProjectIdAsync(
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Images.Where(i => i.ProjectId == projectId);

        var total = await query.CountAsync(cancellationToken);

        var images = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (images, total);
    }

    public Task DeleteAsync(Image image, CancellationToken cancellationToken = default)
    {
        _context.Images.Remove(image);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
