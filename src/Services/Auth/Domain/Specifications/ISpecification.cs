namespace PixPro.Services.Auth.Domain.Specifications;

public interface ISpecification<in T>
{
    Task<bool> IsSatisfiedByAsync(T candidate, CancellationToken cancellationToken = default);
}
