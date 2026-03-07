using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Winnow.Server.Domain;

namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// EF Core interceptor that automatically dispatches domain events after every successful save.
/// Any aggregate implementing IAggregateRoot gets its events dispatched with no manual wiring.
/// Events are cleared from the aggregate before publishing to prevent double-dispatch
/// if a handler triggers another save on the same instance.
/// </summary>
public class DomainEventInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData data,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (data.Context is not null)
        {
            var aggregates = data.Context.ChangeTracker
                .Entries<IAggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            foreach (var aggregate in aggregates)
            {
                // Snapshot then clear before publishing — prevents double-dispatch
                // if a handler triggers another SaveChanges on the same aggregate.
                var events = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents();

                foreach (var domainEvent in events)
                    await publisher.Publish(domainEvent, cancellationToken);
            }
        }

        return await base.SavedChangesAsync(data, result, cancellationToken);
    }
}
