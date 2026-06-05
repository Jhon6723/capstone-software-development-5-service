using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Enums;

namespace PixPro.Services.Projects.Domain.Repositories;

public interface IUserCreditRepository
{
    Task<UserCredit?> GetByUserAndModelAsync(Guid userId, ModelTier modelTier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserCredit>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserCredit userCredit, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
