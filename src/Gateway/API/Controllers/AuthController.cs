using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Middleware;

namespace API.Controllers;

/// <summary>
/// Authentication endpoints (proxied to Auth service)
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Register a new user
    /// </summary>
    /// <remarks>
    /// Creates a new user account with email and password.
    /// No authentication required.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        // This method is never executed - YARP proxies the request
        throw new NotImplementedException("This endpoint is proxied by YARP");
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <remarks>
    /// Authenticates a user and returns a JWT token.
    /// No authentication required.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP");
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <remarks>
    /// Returns Auth0 user claims extracted from the validated token.
    /// </remarks>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        throw new NotImplementedException("This endpoint is proxied by YARP");
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    [HttpGet("users/{email}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByEmail(
        string email,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP");
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() =>
        throw new NotImplementedException("This endpoint is proxied by YARP");

}
