using System.Collections.Concurrent;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.MultiTenancy;


// Middleware for identifying the current tenant based on the request host
public class TenantMiddleware(RequestDelegate next)
{
    private static readonly ConcurrentDictionary<string, bool> _initializedTenants = new();

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, WinnowDbContext dbContext)
    {
        // 1. Check Header First (Preferred for API/Dev)
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantId))
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

        // 3. Ensure Database is initialized for this tenant (once per session)
        var currentTenantId = tenantContext.TenantId ?? "default";
        if (!_initializedTenants.ContainsKey(currentTenantId))
        {
            await dbContext.Database.EnsureCreatedAsync();
            _initializedTenants.TryAdd(currentTenantId, true);
        }

        await next(context);
    }
}
