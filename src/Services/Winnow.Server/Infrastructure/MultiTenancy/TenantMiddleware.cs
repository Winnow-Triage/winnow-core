using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.MultiTenancy;


// Middleware for identifying the current tenant based on the request host
internal class TenantMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private static readonly ConcurrentDictionary<string, bool> _initializedTenants = new();
    private static readonly SemaphoreSlim _migrationLock = new(1, 1);

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, WinnowDbContext dbContext)
    {
        // 0. Check JWT Claims (Most Secure)
        var tenantClaim = context.User.FindFirst("tenant_id");
        if (tenantClaim != null)
        {
            ((TenantContext)tenantContext).TenantId = tenantClaim.Value;
        }

        // Extract organization ID from JWT claim if present
        var organizationClaim = context.User.FindFirst("organization");
        if (organizationClaim != null && Guid.TryParse(organizationClaim.Value, out var organizationId))
        {
            ((TenantContext)tenantContext).CurrentOrganizationId = organizationId;
        }

        // 1. Check Header First (Preferred for API/Dev)
        else if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantId))
        {
            ((TenantContext)tenantContext).TenantId = tenantId;
        }
        else
        {
            // 2. Check Hostname
            var host = context.Request.Host.Host;
            var parts = host.Split('.');

            if (parts.Length > 0 && parts[0] != "localhost" && !System.Net.IPAddress.TryParse(host, out _))
            {
                // Assumes format: {tenant}.domain.com
                if (parts[0] != "www")
                {
                    ((TenantContext)tenantContext).TenantId = parts[0];
                }
            }
        }

        // 3. Ensure Database is initialized for this tenant (once per session).
        //    Skip in Testing environment — tests use EnsureCreated to build schema
        //    from the current model, which is more reliable than migrations.
        if (!env.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            var currentTenantId = tenantContext.TenantId ?? "default";
            if (!_initializedTenants.ContainsKey(currentTenantId))
            {
                await _migrationLock.WaitAsync();
                try
                {
                    if (!_initializedTenants.ContainsKey(currentTenantId))
                    {
                        try
                        {
                            await dbContext.Database.MigrateAsync();
                        }
                        catch (Exception ex)
                        {
                            // MigrateAsync can fail if migration snapshots are missing (e.g., SQLite).
                            // Fall back to EnsureCreated which creates schema from the current model.
                            Console.Error.WriteLine($"Failed to migrate tenant database {tenantContext.ConnectionString}: {ex.Message}");
                            await dbContext.Database.EnsureCreatedAsync();
                        }
                        _initializedTenants.TryAdd(currentTenantId, true);
                    }
                }
                finally
                {
                    _migrationLock.Release();
                }
            }
        }

        // 4. Enforce Organization Suspension Check
        if (tenantContext.CurrentOrganizationId.HasValue)
        {
            var org = await dbContext.Organizations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == tenantContext.CurrentOrganizationId.Value);

            if (org != null && org.IsSuspended)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { message = "This organization has been suspended by an administrator." });
                return;
            }
        }

        await next(context);
    }
}
