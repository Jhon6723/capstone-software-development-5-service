using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using PixPro.Services.Auth.Application.Services.Implementations;
using PixPro.Services.Auth.Application.Services.Interfaces;

namespace Auth.Application.UnitTests.Services.Auth;

public class JwtTokenGeneratorTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly Guid _testUserId;
    private readonly string _testEmail;

    public JwtTokenGeneratorTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        
        SetupJwtConfiguration(
            secret: "my-super-secret-key-that-is-at-least-32-chars-long",
            issuer: "https://test-issuer.com",
            audience: "https://test-audience.com",
            expirationMinutes: "60"
        );
        
        _jwtTokenGenerator = new JwtTokenGenerator(_mockConfiguration.Object);
        _testUserId = Guid.NewGuid();
        _testEmail = "test@example.com";
    }

    private void SetupJwtConfiguration(string secret, string issuer, string audience, string expirationMinutes)
    {
        _mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns(secret);
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns(issuer);
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns(audience);
        _mockConfiguration.Setup(x => x["Jwt:ExpirationMinutes"]).Returns(expirationMinutes);
    }

    // TEST 1: Verify that a valid token is generated with non empty value
    // Ensures the token generation process produces a string that is not null or whitespace
    [Fact]
    public void GenerateToken_WithValidInputs_ReturnsNonEmptyToken()
    {
        var token = _jwtTokenGenerator.GenerateToken(_testUserId, _testEmail);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    // TEST 2: Verify the token contains the correct User ID claim
    // Extracts the 'sub' claim from the generated token and validates it matches the test user ID
    [Fact]
    public void GenerateToken_WithValidInputs_TokenContainsCorrectUserId()
    {
        var token = _jwtTokenGenerator.GenerateToken(_testUserId, _testEmail);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        
        var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(userIdClaim);
        Assert.Equal(_testUserId.ToString(), userIdClaim.Value);
    }

    // TEST 3: Verify the token contains the correct Email claim
    // Extracts the 'email' claim from the generated token and validates it matches the test email
    [Fact]
    public void GenerateToken_WithValidInputs_TokenContainsCorrectEmail()
    {
        var token = _jwtTokenGenerator.GenerateToken(_testUserId, _testEmail);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        
        var emailClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(_testEmail, emailClaim.Value);
    }

    // TEST 4: Handle missing JWT Secret configuration
    // Simulates missing secret key configuration and expects InvalidOperationException with specific message
    [Fact]
    public void GenerateToken_WhenJwtSecretMissing_ThrowsInvalidOperationException()
    {
        _mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("test-issuer");
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("test-audience");
        _mockConfiguration.Setup(x => x["Jwt:ExpirationMinutes"]).Returns("60");
        
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtTokenGenerator.GenerateToken(_testUserId, _testEmail)
        );
        Assert.Equal("JWT Secret is not configured", exception.Message);
    }

    // TEST 5: Handle missing JWT Issuer configuration
    // Simulates missing issuer configuration and expects InvalidOperationException with specific message
    // Secret is provided with minimum required length (more than 32 characters)
    [Fact]
    public void GenerateToken_WhenJwtIssuerMissing_ThrowsInvalidOperationException()
    {
        _mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns("test-secret-min-32-chars-long-enough-here");
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("test-audience");
        _mockConfiguration.Setup(x => x["Jwt:ExpirationMinutes"]).Returns("60");
        
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtTokenGenerator.GenerateToken(_testUserId, _testEmail)
        );
        Assert.Equal("JWT Issuer is not configured", exception.Message);
    }
}
