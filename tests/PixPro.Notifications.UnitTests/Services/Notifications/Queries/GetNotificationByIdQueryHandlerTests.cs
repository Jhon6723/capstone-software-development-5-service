using FluentAssertions;
using MediatR;
using Moq;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Queries.GetNotificationById;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Enums;

namespace PixPro.Notifications.UnitTests.Services.Notifications.Queries;

public class GetNotificationByIdQueryHandlerTests
{
    private readonly Mock<INotificationReadRepository> _readRepositoryMock;
    private readonly GetNotificationByIdQueryHandler _sut;

    public GetNotificationByIdQueryHandlerTests()
    {
        _readRepositoryMock = new Mock<INotificationReadRepository>();
        _sut = new GetNotificationByIdQueryHandler(_readRepositoryMock.Object);
    }

    #region GetNotificationByIdQueryHandler Tests

    // TEST 1: Verify that a notification can be retrieved successfully by valid ID
    // Ensures that when a valid notification ID is provided, the repository returns
    // the matching notification and a success result with the notification data
    [Fact]
    public async Task Handle_ValidNotificationId_ShouldReturnNotification()
    {
        // Arrange
        var notificationId = "notif-123";
        var expectedNotification = new NotificationResponse(
            Id: notificationId,
            UserId: "user-123",
            Type: NotificationType.InApp,
            Title: "Test Title",
            Message: "Test Message",
            IsRead: false,
            CreatedAt: DateTime.UtcNow,
            Metadata: null
        );

        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedNotification);

        var query = new GetNotificationByIdQuery(notificationId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedNotification);
        _readRepositoryMock.Verify(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that retrieving a notification fails when NotificationId is null
    // Tests that a null NotificationId returns a failure result with an appropriate
    // error message and no repository operation is performed
    [Fact]
    public async Task Handle_NullNotificationId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetNotificationByIdQuery(null!);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("NotificationId is required");
        _readRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 3: Verify that retrieving a notification fails when NotificationId is empty
    // Tests that an empty string NotificationId is treated as invalid and returns
    // a failure result without accessing the repository
    [Fact]
    public async Task Handle_EmptyNotificationId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetNotificationByIdQuery(string.Empty);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("NotificationId is required");
    }

    // TEST 4: Verify that retrieving a notification fails when NotificationId is whitespace
    // Tests that a whitespace-only NotificationId is treated as invalid and returns
    // a failure result without accessing the repository
    [Fact]
    public async Task Handle_WhitespaceNotificationId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetNotificationByIdQuery("   ");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("NotificationId is required");
    }

    // TEST 5: Verify that retrieving a non-existent notification returns a failure
    // Simulates the case where no notification exists for the given ID and expects
    // a "Notification not found" error message
    [Fact]
    public async Task Handle_NotificationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var notificationId = "non-existent-id";
        
        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as NotificationResponse);

        var query = new GetNotificationByIdQuery(notificationId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Notification not found");
        _readRepositoryMock.Verify(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 6: Verify that the handler gracefully handles repository exceptions
    // Simulates a database or connection error during retrieval and expects a
    // failure result with the exception message included in the error
    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var notificationId = "notif-123";
        var exceptionMessage = "Redis connection failed";
        
        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        var query = new GetNotificationByIdQuery(notificationId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"Error retrieving notification: {exceptionMessage}");
    }

    #endregion
}
