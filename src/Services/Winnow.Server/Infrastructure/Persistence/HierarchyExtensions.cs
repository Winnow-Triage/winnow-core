using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Persistence;

public static class HierarchyExtensions
{
    /// <summary>
    /// Resolves the ultimate root master for a given ticket.
    /// If the ticket is already a master, returns its own ID.
    /// Supports recursive traversal to ensure we always hit the top-level root.
    /// </summary>
    public static async Task<Guid> ResolveUltimateMasterAsync(this WinnowDbContext db, Guid ticketId, CancellationToken ct = default)
    {
        var currentId = ticketId;

        while (true)
        {
            var parentId = await db.Tickets
                .AsNoTracking()
                .Where(t => t.Id == currentId)
                .Select(t => t.ParentTicketId)
                .FirstOrDefaultAsync(ct);

            if (parentId == null)
            {
                return currentId;
            }

            // Detect self-reference to prevent infinite loops even if data is corrupt
            if (parentId == currentId)
            {
                return currentId;
            }

            currentId = parentId.Value;
        }
    }
}
