using FluentAssertions;
using Moq;
using PixPro.Services.Notifications.Application.Commands.MarkAllAsRead;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Events;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.UnitTests.Services.Notifications.Commands;

public class MarkAllAsReadCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly MarkAllAsReadCommandHandler _sut;

    public MarkAllAsReadCommandHandlerTests()
    {
        _repositoryMock = new Mock<INotificationRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _sut = new MarkAllAsReadCommandHandler(_repositoryMock.Object, _eventPublisherMock.Object);
    }

    #region MarkAllAsReadCommandHandler Tests

    // TEST 1: Verify that all notifications can be marked as read successfully for a user
    // Ensures that when a valid UserId is provided, the unread count is retrieved,
    // all notifications are marked as read in the repository, and a batch event
    // is published for read database synchronization
    [Fact]
    public async Task Handle_ValidCommand_ShouldMarkAllAsReadAndPublishBatchEvent()
    {
        // Arrange
        var userId = "user123";
        var unreadCount = 5;
        var command = new MarkAllAsReadCommand(userId);

        _repositoryMock.Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(unreadCount);
        _repositoryMock.Setup(x => x.MarkAllAsReadAsync(userId))
            .ReturnsAsync(true);
        _eventPublisherMock.Setup(x => x.PublishAsync(It.IsAny<NotificationBatchReadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.GetUnreadCountAsync(userId), Times.Once);
        _repositoryMock.Verify(x => x.MarkAllAsReadAsync(userId), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishAsync(
            It.Is<NotificationBatchReadEvent>(e => e.UserId == userId && e.NotificationsCount == unreadCount),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that marking all as read fails when UserId is empty
    // Tests that an empty UserId returns a failure result with an appropriate error message
    // and no repository operations are performed
    [Fact]
    public async Task Handle_EmptyUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new MarkAllAsReadCommand("");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
        _repositoryMock.Verify(x => x.MarkAllAsReadAsync(It.IsAny<string>()), Times.Never);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<NotificationBatchReadEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 3: Verify that marking all as read fails when UserId contains only whitespace
    // Tests that a whitespace-only UserId is treated as invalid and returns a failure
    [Fact]
    public async Task Handle_WhiteSpaceUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new MarkAllAsReadCommand("   ");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
    }

    // TEST 4: Verify that marking all as read fails when the repository update fails
    // Simulates a scenario where MarkAllAsReadAsync returns false (no notifications found
    // or all already read) and expects a failure result with appropriate error message
    [Fact]
    public async Task Handle_MarkAllAsReadFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user123";
        var command = new MarkAllAsReadCommand(userId);

        _repositoryMock.Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(3);
        _repositoryMock.Setup(x => x.MarkAllAsReadAsync(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("No notifications found or all already read");
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<NotificationBatchReadEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 5: Verify behavior when there are no unread notifications
    // Simulates GetUnreadCountAsync returning 0 and expects a failure result
    // since there are no notifications to mark as read
    [Fact]
    public async Task Handle_WhenNoUnreadNotifications_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user123";
        var command = new MarkAllAsReadCommand(userId);

        _repositoryMock.Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(0);
        _repositoryMock.Setup(x => x.MarkAllAsReadAsync(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("No notifications found or all already read");
    }

    // TEST 6: Verify that exceptions are caught and handled gracefully
    // Simulates a database error during GetUnreadCountAsync and expects a failure
    // result with the error message containing the exception details
    [Fact]
    public async Task Handle_ExceptionThrown_ShouldReturnFailureWithErrorMessage()
    {
        // Arrange
        var command = new MarkAllAsReadCommand("user123");

        _repositoryMock.Setup(x => x.GetUnreadCountAsync("user123"))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Error marking all notifications as read");
        result.Error.Should().Contain("Database error");
    }

    // TEST 7: Verify edge case where there are zero unread notifications but MarkAllAsReadAsync returns true
    // This documents an edge case that shouldn't happen in practice (if there are zero unread,
    // MarkAllAsReadAsync should typically return false). This test verifies the handler's
    // behavior in this unexpected scenario.
    [Fact]
    public async Task Handle_WithZeroUnreadCountButMarkAllSucceeds_ShouldStillPublishEvent()
    {
        // Arrange
        var userId = "user123";
        var command = new MarkAllAsReadCommand(userId);

        _repositoryMock.Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(0);
        _repositoryMock.Setup(x => x.MarkAllAsReadAsync(userId))
            .ReturnsAsync(true); // This shouldn't happen in practice, but testing the handler's behavior

        _eventPublisherMock.Setup(x => x.PublishAsync(It.IsAny<NotificationBatchReadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _eventPublisherMock.Verify(x => x.PublishAsync(
            It.Is<NotificationBatchReadEvent>(e => e.UserId == userId && e.NotificationsCount == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
