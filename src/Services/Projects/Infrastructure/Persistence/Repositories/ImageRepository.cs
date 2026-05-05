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

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
