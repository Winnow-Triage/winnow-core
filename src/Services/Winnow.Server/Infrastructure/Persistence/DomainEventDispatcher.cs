using MediatR;
using Winnow.Server.Domain.Aggregates;

namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// Dispatches domain events collected by an aggregate via MediatR after a successful database save.
/// Call DispatchAndClearAsync immediately after SaveChangesAsync to ensure events fire only
/// on persisted state changes.
/// </summary>
public class DomainEventDispatcher(IPublisher publisher)
{
    public async Task DispatchAndClearAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        var events = organization.DomainEvents.ToList();
        organization.ClearDomainEvents();

        foreach (var domainEvent in events)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
