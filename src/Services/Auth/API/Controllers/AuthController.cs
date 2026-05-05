using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Auth.Application.DTOs.Responses;
using PixPro.Services.Auth.Application.Common.Results;
using PixPro.Services.Auth.Application.DTOs.Requests;
using PixPro.Services.Auth.Application.Services.Interfaces;
using System.Security.Claims;

namespace PixPro.Services.Auth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">User registration data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user information</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to register user with email: {Email}", request.Email);

        var result = await _authService.RegisterUserAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User registered successfully with ID: {UserId}", result.Value!.Id);
            return CreatedAtAction(nameof(GetByEmail), new { email = result.Value.Email }, result.Value);
        }
        return HandleErrorResult(result.Error!);
    }

    /// <summary>
    /// Login user and generate JWT token
    /// </summary>
    /// <param name="request">User login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var result = await _authService.LoginAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return Ok(result.Value);
        }

        _logger.LogWarning("Login failed for email: {Email}", request.Email);
        return HandleErrorResult(result.Error!);
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
    public async Task<IActionResult> GetByEmail(
        string email,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving user with email: {Email}", email);

        var result = await _authService.GetUserByEmailAsync(email, cancellationToken);
        if (result.IsSuccess) return Ok(result.Value);
        return HandleErrorResult(result.Error!);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Auth0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value
                 ?? User.FindFirst("email")?.Value;
        var name = User.FindFirst("name")?.Value;
        var picture = User.FindFirst("picture")?.Value;

        _logger.LogInformation("Auth0 user authenticated: {Sub}", sub);

        return Ok(new
        {
            sub,
            email,
            name,
            picture,
            authProvider = "Auth0",
            authenticatedAt = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() =>
        Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });

    private IActionResult HandleErrorResult(Error error)
    {
        var errorResponse = new ErrorResponse
        {
            Code = error.Code,
            Message = error.Message,
            Type = error.Type.ToString()
        };

        _logger.LogWarning("Request failed with error: {ErrorCode} - {ErrorMessage}", 
            error.Code, error.Message);

        return error.Type switch
        {
            ErrorType.Validation => BadRequest(errorResponse),
            ErrorType.NotFound => NotFound(errorResponse),
            ErrorType.Conflict => Conflict(errorResponse),
            ErrorType.Unauthorized => Unauthorized(errorResponse),
            _ => StatusCode(StatusCodes.Status500InternalServerError, errorResponse)
        };
    }
}
