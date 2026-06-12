using Microsoft.EntityFrameworkCore;
using PixPro.Services.Auth.Domain.Entities;
using PixPro.Services.Auth.Domain.Repositories;

namespace PixPro.Services.Auth.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AuthDbContext context) : IUserRepository
{
    private readonly AuthDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
