using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Infrastructure.Persistence;

public class WinnowDbContext(DbContextOptions<WinnowDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Dynamic connection string based on tenant
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(tenantContext.ConnectionString);
            optionsBuilder.AddInterceptors(new SqliteVectorConnectionInterceptor());
        }
    }

    public DbSet<Ticket> Tickets { get; set; } = null!;
}
