using PixPro.Services.Auth.Domain.Enums;
using PixPro.Services.Auth.Domain.ValueObjects;

namespace PixPro.Services.Auth.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string? Auth0Id { get; private set; }
    public string Email { get; private set; }
    public string Password { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Block()   => IsActive = false;
    public void Unblock() => IsActive = true;

    public User(string? auth0Id, string email, string hashedPassword, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Hashed password cannot be null or empty.", nameof(hashedPassword));

        var emailVo = new Email(email);

        Id = Guid.NewGuid();
        Auth0Id = string.IsNullOrWhiteSpace(auth0Id) ? null : auth0Id.Trim();
        Email = emailVo.Value;
        Password = hashedPassword;
        Role = role;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private User()
    {
        Auth0Id = null;
        Email = string.Empty;
        Password = string.Empty;
        Role = UserRole.User;
        IsActive = true;
    }

    public void UpdateAuth0Id(string auth0Id)
    {
        if (string.IsNullOrWhiteSpace(auth0Id))
            throw new ArgumentException("Auth0Id cannot be null or empty.", nameof(auth0Id));

        Auth0Id = auth0Id.Trim();
    }
}
