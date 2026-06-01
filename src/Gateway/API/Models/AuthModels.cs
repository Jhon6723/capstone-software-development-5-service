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
    /// Token type
    /// </summary>
    [DefaultValue("Bearer")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    [DefaultValue(3600)]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Authenticated user information
    /// </summary>
    public required UserResponse User { get; set; }
}

/// <summary>
/// User profile information
/// </summary>
public class UserResponse
{
    [DefaultValue("d38ad444-b3d1-458c-bc08-f94bc7c84b78")]
    public Guid Id { get; init; }

    [DefaultValue("auth0|65f3a1b29fcd123456789abc")]
    public string? Auth0Id { get; init; }

    [DefaultValue("test1@example.com")]
    public string Email { get; init; } = string.Empty;

    [DefaultValue("2026-06-01T18:37:35Z")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Current Auth0 user profile returned by /api/auth/me
/// </summary>
public class CurrentUserResponse
{
    [DefaultValue("google-oauth2|112749643959467185814")]
    public string? Sub { get; init; }

    [DefaultValue("jhonestebanrivera66@gmail.com")]
    public string? Email { get; init; }

    [DefaultValue("Jhon Esteban Rivera")]
    public string? Name { get; init; }

    [DefaultValue("https://lh3.googleusercontent.com/a/ACg8oc...=s96-c")]
    public string? Picture { get; init; }

    [DefaultValue("Auth0")]
    public string AuthProvider { get; init; } = "Auth0";

    [DefaultValue("2026-06-01T18:37:35Z")]
    public DateTimeOffset AuthenticatedAt { get; init; }
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
    [DefaultValue("refresh_token_example_123456")]
    public required string RefreshToken { get; set; }
}

