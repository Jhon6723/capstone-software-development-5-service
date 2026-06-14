using Microsoft.EntityFrameworkCore;
using Npgsql;
using PixPro.Services.Projects.Application.Common.Exceptions;
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
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Another transaction modified one of these rows first (xmin changed).
            // Reload the current database values into the tracked entities so the
            // caller can re-apply its logic on fresh data, then surface a
            // provider-agnostic exception for the application layer to retry.
            foreach (var entry in ex.Entries)
            {
                await entry.ReloadAsync(cancellationToken);
            }

            throw new ConcurrencyConflictException(
                "A concurrent update conflict occurred while saving UserCredit changes.", ex);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent request inserted a row with the same (UserId, ModelTier)
            // first. Detach our failed Added entities so a retry re-reads the now
            // existing row instead of attempting another duplicate insert.
            DetachAddedEntries();

            throw new ConcurrencyConflictException(
                "A concurrent insert conflict occurred while saving UserCredit changes.", ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private void DetachAddedEntries()
    {
        var addedEntries = _context.ChangeTracker
            .Entries<UserCredit>()
            .Where(e => e.State == EntityState.Added)
            .ToList();

        foreach (var entry in addedEntries)
        {
            entry.State = EntityState.Detached;
        }
    }
}
