using FluentAssertions;
using Moq;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.Queries.GetUnreadCount;
using PixPro.Services.Notifications.Application.Services;

namespace PixPro.Notifications.UnitTests.Services.Notifications.Queries;

public class GetUnreadCountQueryHandlerTests
{
    private readonly Mock<INotificationReadRepository> _readRepositoryMock;
    private readonly GetUnreadCountQueryHandler _sut;

    public GetUnreadCountQueryHandlerTests()
    {
        _readRepositoryMock = new Mock<INotificationReadRepository>();
        _sut = new GetUnreadCountQueryHandler(_readRepositoryMock.Object);
    }

    #region GetUnreadCountQueryHandler Tests

    // TEST 1: Verify that unread count can be retrieved successfully for a valid user
    // Ensures that when a valid UserId is provided, the repository returns the
    // correct unread count and a success result with the count value
    [Fact]
    public async Task Handle_ValidUserId_ShouldReturnUnreadCount()
    {
        // Arrange
        var userId = "user-123";
        var expectedCount = 5;
        
        _readRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        var query = new GetUnreadCountQuery(userId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedCount);
        _readRepositoryMock.Verify(x => x.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that zero unread count is handled correctly
    // Tests the edge case where a user has no unread notifications and expects
    // a success result with a count of zero
    [Fact]
    public async Task Handle_ZeroUnreadCount_ShouldReturnZero()
    {
        // Arrange
        var userId = "user-123";
        
        _readRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetUnreadCountQuery(userId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    // TEST 3: Verify that retrieving unread count fails when UserId is null
    // Tests that a null UserId returns a failure result with an appropriate
    // error message and no repository operation is performed
    [Fact]
    public async Task Handle_NullUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUnreadCountQuery(null!);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
        _readRepositoryMock.Verify(x => x.GetUnreadCountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 4: Verify that retrieving unread count fails when UserId is empty
    // Tests that an empty string UserId is treated as invalid and returns
    // a failure result without accessing the repository
    [Fact]
    public async Task Handle_EmptyUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUnreadCountQuery(string.Empty);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
    }

    // TEST 5: Verify that retrieving unread count fails when UserId is whitespace
    // Tests that a whitespace-only UserId is treated as invalid and returns
    // a failure result without accessing the repository
    [Fact]
    public async Task Handle_WhitespaceUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUnreadCountQuery("   ");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
    }

    // TEST 6: Verify that the handler gracefully handles repository exceptions
    // Simulates a database or connection error during retrieval and expects a
    // failure result with the exception message included in the error
    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-123";
        var exceptionMessage = "Redis connection timeout";
        
        _readRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        var query = new GetUnreadCountQuery(userId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"Error retrieving unread count: {exceptionMessage}");
    }

    // TEST 7: Verify that large unread count values are handled correctly
    // Tests that the handler can handle large integer values without overflow
    // or other numeric issues
    [Fact]
    public async Task Handle_LargeUnreadCount_ShouldHandleCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var largeCount = 9999;
        
        _readRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(largeCount);

        var query = new GetUnreadCountQuery(userId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(largeCount);
    }

    // TEST 8: Verify that consecutive calls do not use caching at handler level
    // Ensures that each call to the handler results in a repository call since
    // no caching is implemented at this level (cache may exist in Redis repository)
    [Fact]
    public async Task Handle_ConsecutiveCalls_ShouldCallRepositoryEachTime()
    {
        // Arrange
        var userId = "user-123";
        
        _readRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetUnreadCountQuery(userId);

        // Act
        await _sut.Handle(query, CancellationToken.None);
        await _sut.Handle(query, CancellationToken.None);
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _readRepositoryMock.Verify(x => x.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion
}
