using PixPro.Services.Auth.Domain.ValueObjects;

namespace PixPro.Services.Auth.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string? Auth0Id { get; private set; }
    public string Email { get; private set; }
    public string Password { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User(string? auth0Id, string email, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Hashed password cannot be null or empty.", nameof(hashedPassword));

        var emailVo = new Email(email);

        Id = Guid.NewGuid();
        Auth0Id = string.IsNullOrWhiteSpace(auth0Id) ? null : auth0Id.Trim();
        Email = emailVo.Value;
        Password = hashedPassword;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private User()
    {
        Auth0Id = null;
        Email = string.Empty;
        Password = string.Empty;
    }

    public void UpdateAuth0Id(string auth0Id)
    {
        if (string.IsNullOrWhiteSpace(auth0Id))
            throw new ArgumentException("Auth0Id cannot be null or empty.", nameof(auth0Id));

        Auth0Id = auth0Id.Trim();
    }
}
