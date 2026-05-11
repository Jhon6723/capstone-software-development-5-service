using PixPro.Services.Notifications.Domain.Events;

namespace PixPro.Services.Notifications.Application.Services;

/// <summary>
/// Service for publishing domain events to the message broker
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to the message broker for read database synchronization
    /// </summary>
    /// <typeparam name="TEvent">Type of domain event</typeparam>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : NotificationDomainEvent;
}
