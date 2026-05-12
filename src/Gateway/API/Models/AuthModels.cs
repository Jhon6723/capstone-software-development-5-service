using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace API.Models;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required]
    [EmailAddress]
    [DefaultValue("test1@example.com")]
    public required string Email { get; set; }

    /// <summary>
    /// User's password (min 8 characters)
    /// </summary>
    [Required]
    [MinLength(8)]
    [DefaultValue("Test123!")]
    public required string Password { get; set; }
}

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required]
    [EmailAddress]
    [DefaultValue("test1@example.com")]
    public required string Email { get; set; }

    /// <summary>
    /// User's password
    /// </summary>
    [Required]
    [DefaultValue("Test123!")]
    public required string Password { get; set; }
}

/// <summary>
/// Response model after successful authentication
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    [DefaultValue("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")]
    public required string Token { get; set; }

    /// <summary>
    /// User's unique identifier
    /// </summary>
    [DefaultValue("d38ad444-b3d1-458c-bc08-f94bc7c84b78")]
    public required string UserId { get; set; }

    /// <summary>
    /// User's email
    /// </summary>
    [DefaultValue("test1@example.com")]
    public required string Email { get; set; }

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    [DefaultValue(3600)]
    public int ExpiresIn { get; set; }
}

/// <summary>
/// User profile information
/// </summary>
public class UserResponse
{
    public Guid Id { get; init; }
    public string? Auth0Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}


/// <summary>
/// Request model for refreshing JWT token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token obtained during login
    /// </summary>
    [Required]
    public required string RefreshToken { get; set; }
}

