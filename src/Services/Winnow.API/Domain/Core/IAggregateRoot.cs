namespace Winnow.API.Domain.Core;

/// <summary>
/// Marker interface for aggregate roots. Aggregates implementing this interface
/// automatically have their domain events dispatched by DomainEventInterceptor
/// after each successful SaveChangesAsync call.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
