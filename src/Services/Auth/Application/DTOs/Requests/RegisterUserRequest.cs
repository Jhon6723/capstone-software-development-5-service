using System.ComponentModel.DataAnnotations;

namespace PixPro.Services.Auth.Application.DTOs.Requests;

public sealed record RegisterUserRequest
{
    public string? Auth0Id { get; init; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = string.Empty;
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; init; } = string.Empty;
}
