using Microsoft.EntityFrameworkCore;
using PixPro.Services.Projects.Domain.Entities;

namespace PixPro.Services.Projects.Infrastructure.Persistence;

public sealed class ProjectsDbContext(DbContextOptions<ProjectsDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<UserCredit> UserCredits => Set<UserCredit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectsDbContext).Assembly);
    }
}
