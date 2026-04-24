using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Auth.Application.Common.Results;
using PixPro.Services.Auth.Application.DTOs.Requests;
using PixPro.Services.Auth.Application.DTOs.Responses;
using PixPro.Services.Auth.Application.Services.Interfaces;

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
            return CreatedAtAction(
                nameof(GetByEmail),
                new { email = result.Value.Email },
                result.Value);
        }

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
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByEmail(
        string email,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving user with email: {Email}", email);

        var result = await _authService.GetUserByEmailAsync(email, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return HandleErrorResult(result.Error!);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
    }

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
