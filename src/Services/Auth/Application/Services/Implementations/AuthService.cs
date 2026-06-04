using System.Text.RegularExpressions;
using PixPro.Services.Auth.Application.Common.Results;
using PixPro.Services.Auth.Application.DTOs.Requests;
using PixPro.Services.Auth.Application.DTOs.Responses;
using PixPro.Services.Auth.Application.Services.Interfaces;
using PixPro.Services.Auth.Domain.Entities;
using PixPro.Services.Auth.Domain.Repositories;
using PixPro.Services.Auth.Domain.Specifications;

namespace PixPro.Services.Auth.Application.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    private static Result ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Error.Validation(
                "Password.Required",
                "Password is required.");
        }

        if (password.Length < 8)
        {
            return Error.Validation(
                "Password.TooShort",
                "Password must have at least 8 characters.");
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            return Error.Validation(
                "Password.MissingUpperCase",
                "Password must contain at least 1 capital letter.");
        }

        if (!Regex.IsMatch(password, @"[0-9]"))
        {
            return Error.Validation(
                "Password.MissingNumber",
                "Password must contain at least 1 number.");
        }

        return Result.Success();
    }

    public async Task<Result<UserResponse>> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate password requirements
            var passwordValidation = ValidatePassword(request.Password);
            if (!passwordValidation.IsSuccess)
            {
                return Result<UserResponse>.Failure(passwordValidation.Error!);
            }

            // Validate email uniqueness using Specification
            var uniqueEmailSpec = new UniqueEmailSpecification(_userRepository);
            var isEmailUnique = await uniqueEmailSpec.IsSatisfiedByAsync(request.Email, cancellationToken);

            if (!isEmailUnique)
            {
                return Error.Conflict(
                    "User.EmailAlreadyExists",
                    $"A user with email '{request.Email}' already exists.");
            }

            // Hash password before creating entity
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create User entity with hashed password
            var user = new User(request.Auth0Id, request.Email, hashedPassword);

            // Save to repository
            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Map to response
            var response = new UserResponse
            {
                Id = user.Id,
                Auth0Id = user.Auth0Id,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return Result<UserResponse>.Success(response);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(
                "User.InvalidData",
                ex.Message);
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "User.RegistrationFailed",
                $"An error occurred while registering the user: {ex.Message}");
        }
    }

    public async Task<Result<UserResponse>> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user is null)
            {
                return Error.NotFound(
                    "User.NotFound",
                    $"User with email '{email}' was not found.");
            }

            var response = new UserResponse
            {
                Id = user.Id,
                Auth0Id = user.Auth0Id,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return Result<UserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "User.RetrievalFailed",
                $"An error occurred while retrieving the user: {ex.Message}");
        }
    }

    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find user by email
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null)
            {
                return Error.Unauthorized(
                    "Auth.InvalidCredentials",
                    "Invalid credentials.");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Error.Unauthorized(
                    "Auth.InvalidCredentials",
                    "Invalid credentials.");
            }

            // Generate JWT token
            var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Role.ToString());

            // Create response
            var response = new LoginResponse
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = 86400, // 24 hours in seconds
                User = new UserResponse
                {
                    Id = user.Id,
                    Auth0Id = user.Auth0Id,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            };

            return Result<LoginResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "Auth.LoginFailed",
                $"An error occurred during login: {ex.Message}");
        }
    }
}
