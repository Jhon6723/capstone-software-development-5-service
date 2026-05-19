using FluentAssertions;
using Moq;
using PixPro.Services.Notifications.Application.Commands.MarkAsRead;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Entities;
using PixPro.Services.Notifications.Domain.Events;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.UnitTests.Services.Notifications.Commands;

public class MarkAsReadCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly MarkAsReadCommandHandler _sut;

    public MarkAsReadCommandHandlerTests()
    {
        _repositoryMock = new Mock<INotificationRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _sut = new MarkAsReadCommandHandler(_repositoryMock.Object, _eventPublisherMock.Object);
    }

    #region MarkAsReadCommandHandler Tests

    // TEST 1: Verify that a notification can be marked as read successfully
    // Ensures that when a valid notification ID is provided, the notification is retrieved,
    // marked as read in the repository, and a domain event is published for read database sync
    [Fact]
    public async Task Handle_ValidCommand_ShouldMarkAsReadAndPublishEvent()
    {
        // Arrange
        var notificationId = "notif123";
        var userId = "user123";
        var command = new MarkAsReadCommand(notificationId);

        var existingNotification = new Notification
        {
            Id = notificationId,
            UserId = userId
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(notificationId))
            .ReturnsAsync(existingNotification);
        _repositoryMock.Setup(x => x.MarkAsReadAsync(notificationId))
            .ReturnsAsync(true);
        _eventPublisherMock.Setup(x => x.PublishAsync(It.IsAny<NotificationReadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.GetByIdAsync(notificationId), Times.Once);
        _repositoryMock.Verify(x => x.MarkAsReadAsync(notificationId), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishAsync(
            It.Is<NotificationReadEvent>(e => e.NotificationId == notificationId && e.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that marking as read fails when NotificationId is empty
    // Tests that an empty NotificationId returns a failure result with an appropriate error message
    // and no repository operations are performed
    [Fact]
    public async Task Handle_EmptyNotificationId_ShouldReturnFailure()
    {
        // Arrange
        var command = new MarkAsReadCommand("");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("NotificationId is required");
        _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(x => x.MarkAsReadAsync(It.IsAny<string>()), Times.Never);
    }

    // TEST 3: Verify that marking as read fails when the notification does not exist
    // Simulates a non-existent notification ID and expects a failure result
    // with the specific error message "Notification not found"
    [Fact]
    public async Task Handle_NotificationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new MarkAsReadCommand("nonexistent");

        _repositoryMock.Setup(x => x.GetByIdAsync("nonexistent"))
            .ReturnsAsync((Notification?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Notification not found");
        _repositoryMock.Verify(x => x.MarkAsReadAsync(It.IsAny<string>()), Times.Never);
    }

    // TEST 4: Verify that marking as read fails when the repository update fails
    // Simulates a scenario where MarkAsReadAsync returns false (notification not found
    // or already read) and expects a failure result with appropriate error message
    [Fact]
    public async Task Handle_MarkAsReadFails_ShouldReturnFailure()
    {
        // Arrange
        var notificationId = "notif123";
        var command = new MarkAsReadCommand(notificationId);

        var existingNotification = new Notification
        {
            Id = notificationId,
            UserId = "user123"
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(notificationId))
            .ReturnsAsync(existingNotification);
        _repositoryMock.Setup(x => x.MarkAsReadAsync(notificationId))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Notification not found or already read");
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<NotificationReadEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 5: Verify that exceptions are caught and handled gracefully
    // Simulates a database error during GetByIdAsync and expects a failure result
    // with the error message containing the exception details
    [Fact]
    public async Task Handle_ExceptionThrown_ShouldReturnFailureWithErrorMessage()
    {
        // Arrange
        var command = new MarkAsReadCommand("notif123");

        _repositoryMock.Setup(x => x.GetByIdAsync("notif123"))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Error marking notification as read");
        result.Error.Should().Contain("Database error");
    }

    #endregion
}
