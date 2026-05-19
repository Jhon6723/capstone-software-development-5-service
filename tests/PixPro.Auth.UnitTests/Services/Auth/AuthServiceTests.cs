using System;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using FluentAssertions;
using Moq;
using PixPro.Services.Auth.Application.Common.Results;
using PixPro.Services.Auth.Application.DTOs.Requests;
using PixPro.Services.Auth.Application.DTOs.Responses;
using PixPro.Services.Auth.Application.Services.Implementations;
using PixPro.Services.Auth.Application.Services.Interfaces;
using PixPro.Services.Auth.Domain.Entities;
using PixPro.Services.Auth.Domain.Repositories;

namespace PixPro.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _authService = new AuthService(_userRepositoryMock.Object, _jwtTokenGeneratorMock.Object);
    }

    #region RegisterUserAsync Tests

    // TEST 1: Verify that a user can register successfully with valid data
    // Ensures that when valid credentials are provided and email doesn't exist,
    // the user is created, saved to repository, and returns a success result with user data
    [Fact]
    public async Task RegisterUserAsync_WithValidData_ShouldRegisterUserSuccessfully()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Auth0Id = "auth0|123456",
            Email = "test@example.com",
            Password = "ValidPass123"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _authService.RegisterUserAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(request.Email);
        result.Value.Auth0Id.Should().Be(request.Auth0Id);
        
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that registration fails when email already exists
    // Simulates an existing user in the repository and expects a Conflict error
    // with the specific error code 'User.EmailAlreadyExists'
    [Fact]
    public async Task RegisterUserAsync_WhenEmailAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Auth0Id = "auth0|123456",
            Email = "existing@example.com",
            Password = "ValidPass123"
        };

        var existingUser = new User("auth0|old", request.Email, BCrypt.Net.BCrypt.HashPassword("OldPass123"));

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterUserAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("User.EmailAlreadyExists");
        result.Error.Message.Should().Contain(request.Email);
        
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 3: Verify that registration fails with invalid email format
    // The UniqueEmailSpecification returns false for invalid emails which is interpreted 
    // as "email already exists", causing a Conflict error instead of Validation
    [Fact]
    public async Task RegisterUserAsync_WithInvalidEmail_ShouldReturnConflictError()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Auth0Id = "auth0|123456",
            Email = "invalid-email",
            Password = "ValidPass123"
        };
        
        // Act
        var result = await _authService.RegisterUserAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("User.EmailAlreadyExists");
        
        _userRepositoryMock.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 4: Verify password validation rules are enforced during registration
    // Tests multiple scenarios: empty password, too short, missing uppercase, missing number
    // Each invalid password format should return a Validation error with specific error code
    [Fact]
    public async Task RegisterUserAsync_WithInvalidPassword_ShouldReturnValidationError()
    {
        // Arrange
        var testCases = new[]
        {
            new { Password = "", ErrorCode = "Password.Required", Description = "Empty password" },
            new { Password = "short", ErrorCode = "Password.TooShort", Description = "Too short password" },
            new { Password = "nouppercase123", ErrorCode = "Password.MissingUpperCase", Description = "No uppercase letter" },
            new { Password = "NoNumbersHere", ErrorCode = "Password.MissingNumber", Description = "No numbers" },
            new { Password = "lowercase123", ErrorCode = "Password.MissingUpperCase", Description = "No uppercase letter" },
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Auth0Id = "auth0|123456",
                Email = "test@example.com",
                Password = testCase.Password
            };

            // Act
            var result = await _authService.RegisterUserAsync(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse(testCase.Description);
            result.Error.Should().NotBeNull();
            result.Error!.Type.Should().Be(ErrorType.Validation);
            result.Error.Code.Should().Be(testCase.ErrorCode);
            
            _userRepositoryMock.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
            
            // Reset for next test case
            _userRepositoryMock.Invocations.Clear();
        }
    }

    #endregion

    #region LoginAsync Tests

    // TEST 5: Verify that a user can login successfully with valid credentials
    // Ensures that when correct email and password are provided, the service returns
    // a success result with a valid JWT token and user information
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldLoginSuccessfullyAndReturnToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "ValidPass123"
        };

        var userId = Guid.NewGuid();
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User("auth0|123", request.Email, hashedPassword);
        
        // Use reflection to set Id since it's private
        typeof(User).GetProperty("Id")?.SetValue(user, userId);

        var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateToken(user.Id, user.Email))
            .Returns(expectedToken);

        // Act
        var result = await _authService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be(expectedToken);
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.ExpiresIn.Should().Be(86400);
        result.Value.User.Should().NotBeNull();
        result.Value.User!.Email.Should().Be(request.Email);
        
        _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(user.Id, user.Email), Times.Once);
    }

    // TEST 6: Verify login fails when user does not exist
    // Simulates a non-existent email in the repository and expects an Unauthorized error
    // with the specific message "Invalid credentials"
    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ShouldReturnUnauthorizedError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword123"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Message.Should().Be("Invalid credentials.");
        
        _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    // TEST 7: Verify login fails with incorrect password
    // Simulates existing user but with wrong password and expects Unauthorized error
    // The password verification should fail even though user exists
    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ShouldReturnUnauthorizedError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123"
        };

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPass123");
        var user = new User("auth0|123", request.Email, hashedPassword);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Message.Should().Be("Invalid credentials.");
        
        _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    // TEST 8: Handle invalid email format during login
    // The repository's GetByEmailAsync likely catches the ArgumentException from the Email constructor
    // and returns null, resulting in an Unauthorized error instead of a validation error
    [Fact]
    public async Task LoginAsync_WithInvalidEmailFormat_ShouldReturnUnauthorizedError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "invalid-email-format",
            Password = "ValidPass123"
        };
        
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        
        _userRepositoryMock.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    #endregion
}
