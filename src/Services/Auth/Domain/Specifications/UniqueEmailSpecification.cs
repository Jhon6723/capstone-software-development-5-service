using PixPro.Services.Auth.Domain.Entities;
using PixPro.Services.Auth.Domain.Repositories;
using PixPro.Services.Auth.Domain.ValueObjects;

namespace PixPro.Services.Auth.Domain.Specifications;

public sealed class UniqueEmailSpecification(IUserRepository userRepository) : ISpecification<string>
{
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public async Task<bool> IsSatisfiedByAsync(string candidate, CancellationToken cancellationToken = default)
    {
        Email email;
        try
        {
            email = new Email(candidate);
        }
        catch (ArgumentException)
        {
            return false;
        }

        User? existing = await _userRepository.GetByEmailAsync(email.Value, cancellationToken);
        return existing is null;
    }
}
