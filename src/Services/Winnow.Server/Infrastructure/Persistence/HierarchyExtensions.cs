using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Persistence;

public static class HierarchyExtensions
{
    public static async Task<Guid> ResolveUltimateMasterAsync(this WinnowDbContext db, Guid reportId, CancellationToken ct = default)
    {
        var currentId = reportId;

        while (true)
        {
            var parentId = await db.Reports
                .AsNoTracking()
                .Where(t => t.Id == currentId)
                .Select(t => t.ParentReportId)
                .FirstOrDefaultAsync(ct);

            if (parentId == null)
            {
                return currentId;
            }

            if (parentId == currentId)
            {
                return currentId;
            }

            currentId = parentId.Value;
        }
    }
}
