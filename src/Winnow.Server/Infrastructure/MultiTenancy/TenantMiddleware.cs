namespace Winnow.Server.Infrastructure.MultiTenancy;


// Middleware for identifying the current tenant based on the request host
public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // simplistic subdomain parsing: tenant.winnow.app
        var host = context.Request.Host.Host;
        var parts = host.Split('.');

        if (parts.Length > 0 && parts[0] != "localhost" && !System.Net.IPAddress.TryParse(host, out _))
        {
            // Assumes format: {tenant}.domain.com or just {tenant} for local testing via hosts file
            // For localhost, we might default to null (default.db)
            if (parts[0] != "www")
            {
                ((TenantContext)tenantContext).TenantId = parts[0];
            }
        }

        await next(context);
    }
}
