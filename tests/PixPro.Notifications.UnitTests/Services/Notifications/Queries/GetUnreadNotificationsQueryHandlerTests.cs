using FluentAssertions;
using Moq;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Queries.GetUnreadNotifications;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Enums;

namespace PixPro.Notifications.UnitTests.Services.Notifications.Queries;

public class GetUnreadNotificationsQueryHandlerTests
{
    private readonly Mock<INotificationReadRepository> _readRepositoryMock;
    private readonly GetUnreadNotificationsQueryHandler _sut;

    public GetUnreadNotificationsQueryHandlerTests()
    {
        _readRepositoryMock = new Mock<INotificationReadRepository>();
        _sut = new GetUnreadNotificationsQueryHandler(_readRepositoryMock.Object);
    }

    #region GetUnreadNotificationsQueryHandler Tests

    // TEST 1: Verify that unread notifications can be retrieved successfully for a valid user
    // Ensures that when a valid UserId is provided, the repository returns all
    // unread notifications and a success result with the complete list
    [Fact]
    public async Task Handle_ValidUserId_ShouldReturnUnreadNotifications()
    {
        // Arrange
        var userId = "user-123";
        var expectedNotifications = new List<NotificationResponse>
        {
            new NotificationResponse(
                Id: "notif-1",
                UserId: userId,
                Type: NotificationType.InApp,
                Title: "Title 1",
                Message: "Message 1",
                IsRead: false,
                CreatedAt: DateTime.UtcNow,
                Metadata: null
            ),
            new NotificationResponse(
                Id: "notif-2",
                UserId: userId,
                Type: NotificationType.Email,
                Title: "Title 2",
                Message: "Message 2",
                IsRead: false,
                CreatedAt: DateTime.UtcNow,
                Metadata: new Dictionary<string, string> { { "key", "value" } }
            )
        };

        _readRepositoryMock
            .Setup(x => x.GetUnreadByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedNotifications);

        var query = new GetUnreadNotificationsQuery(userId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedNotifications);
        result.Value.Should().HaveCount(2);
        _readRepositoryMock.Verify(x => x.GetUnreadByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that a user with no unread notifications receives an empty list
    // Tests the edge case where a user has all notifications read and expects
    // a success result with an empty collection rather than null
    [Fact]
    public async Task Handle_NoUnreadNotifications_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = "user-123";
        var emptyList = new List<NotificationResponse>();

        _readRepositoryMock
            .Setup(x => x.GetUnreadByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        var query = new GetUnreadNotificationsQuery(userId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // TEST 3: Verify that large volumes of unread notifications are handled correctly
    // Tests the handler's ability to process 50 unread notifications without
    // performance issues or data loss
    [Fact]
    public async Task Handle_ValidUserIdWithMultipleNotifications_ShouldReturnAllUnread()
    {
        // Arrange
        var userId = "user-123";
        var notifications = new List<NotificationResponse>();
        
        for (int i = 1; i <= 50; i++)
        {
            notifications.Add(new NotificationResponse(
                Id: $"notif-{i}",
                UserId: userId,
                Type: NotificationType.InApp,
                Title: $"Title {i}",
                Message: $"Message {i}",
                IsRead: false,
                CreatedAt: DateTime.UtcNow.AddMinutes(-i),
                Metadata: null
            ));
        }

        _readRepositoryMock
            .Setup(x => x.GetUnreadByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);

        var query = new GetUnreadNotificationsQuery(userId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(50);
    }

    // TEST 4: Verify that the handler propagates repository exceptions
    // Simulates a database or connection error during retrieval and expects
    // the exception to be thrown without being caught by the handler
    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldThrowException()
    {
        // Arrange
        var userId = "user-123";
        var exceptionMessage = "Redis connection failed";
        
        _readRepositoryMock
            .Setup(x => x.GetUnreadByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        var query = new GetUnreadNotificationsQuery(userId);

        // Act & Assert
        var act = async () => await _sut.Handle(query, CancellationToken.None);
        
        await act.Should().ThrowAsync<Exception>()
            .WithMessage(exceptionMessage);
    }

    // TEST 5: Verify that null UserId throws an ArgumentNullException
    // Tests that passing a null UserId results in an ArgumentNullException
    // since the handler does not validate the input before calling the repository
    [Fact]
    public async Task Handle_NullUserId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var query = new GetUnreadNotificationsQuery(null!);

        _readRepositoryMock
            .Setup(x => x.GetUnreadByUserIdAsync(null!, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentNullException());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.Handle(query, CancellationToken.None));
    }

    // TEST 6: Verify that consecutive calls do not use caching at handler level
    // Ensures that each call to the handler results in a repository call since
    // no caching is implemented at this level (cache may exist in Redis repository)
    [Fact]
    public async Task Handle_ConsecutiveCalls_ShouldCallRepositoryEachTime()
    {
        // Arrange
        var userId = "user-123";
        var notifications = new List<NotificationResponse>
        {
            new NotificationResponse("1", userId, NotificationType.InApp, "Title", "Message", false, DateTime.UtcNow, null)
        };

        _readRepositoryMock
            .Setup(x => x.GetUnreadByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);

        var query = new GetUnreadNotificationsQuery(userId);

        // Act
        await _sut.Handle(query, CancellationToken.None);
        await _sut.Handle(query, CancellationToken.None);
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _readRepositoryMock.Verify(x => x.GetUnreadByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion
}
