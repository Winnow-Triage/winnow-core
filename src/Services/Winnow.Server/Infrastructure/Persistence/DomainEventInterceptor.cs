using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Winnow.Server.Domain.Core;

namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// EF Core interceptor that automatically dispatches domain events PRE-SAVE.
/// Any aggregate implementing IAggregateRoot gets its events dispatched synchronously.
/// Because this hooks into SavingChangesAsync, MediatR handlers can mutate other aggregates,
/// and those mutations will be included in the original database transaction.
/// </summary>
public sealed class DomainEventInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            // 1. Grab all aggregates with pending events
            var aggregates = eventData.Context.ChangeTracker
                .Entries<IAggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            // 2. Extract the events
            var domainEvents = aggregates.SelectMany(x => x.DomainEvents).ToList();

            // 3. Clear them from the entities to prevent double-dispatch loops
            aggregates.ForEach(a => a.ClearDomainEvents());

            // 4. Fire the synchronous MediatR handlers
            foreach (var domainEvent in domainEvents)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }
        }

        // 5. Proceed with the actual database save
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}