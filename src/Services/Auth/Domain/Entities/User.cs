using PixPro.Services.Auth.Domain.ValueObjects;

namespace PixPro.Services.Auth.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Auth0Id { get; private set; }
    public string Email { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User(string auth0Id, string email)
    {
        if (string.IsNullOrWhiteSpace(auth0Id))
            throw new ArgumentException("Auth0Id cannot be null or empty.", nameof(auth0Id));

        var emailVo = new Email(email);

        Id = Guid.NewGuid();
        Auth0Id = auth0Id.Trim();
        Email = emailVo.Value;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private User()
    {
        Auth0Id = string.Empty;
        Email = string.Empty;
    }
}
