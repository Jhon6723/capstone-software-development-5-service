using Microsoft.EntityFrameworkCore;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.Services.Projects.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository(ProjectsDbContext context) : IProjectRepository
{
    private readonly ProjectsDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
