namespace PixPro.Services.Auth.Application.DTOs.Responses;

public sealed record UserResponse
{
    public Guid Id { get; init; }
    public string Auth0Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
