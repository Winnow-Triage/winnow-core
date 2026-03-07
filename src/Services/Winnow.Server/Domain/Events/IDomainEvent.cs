using MediatR;

namespace Winnow.Server.Domain.Events;

/// <summary>
/// Marker interface for all domain events.
/// Extends MediatR's INotification so events can be dispatched via IPublisher.
/// </summary>
#pragma warning disable CA1040 // Marker interface pattern is intentional here
public interface IDomainEvent : INotification { }
#pragma warning restore CA1040