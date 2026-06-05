using Microsoft.EntityFrameworkCore;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Enums;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.Services.Projects.Infrastructure.Persistence.Repositories;

public sealed class UserCreditRepository(ProjectsDbContext context) : IUserCreditRepository
{
    private readonly ProjectsDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<UserCredit?> GetByUserAndModelAsync(
        Guid userId,
        ModelTier modelTier,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserCredits
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ModelTier == modelTier, cancellationToken);
    }

    public async Task<IReadOnlyList<UserCredit>> GetAllByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserCredits
            .Where(uc => uc.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserCredit userCredit, CancellationToken cancellationToken = default)
    {
        await _context.UserCredits.AddAsync(userCredit, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
