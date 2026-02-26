using FastEndpoints;
using Winnow.Server.Services.Quota;

namespace Winnow.Server.Infrastructure.Security;

public class QuotaEnforcementPreProcessor<TRequest> : IPreProcessor<TRequest>
{
    public async Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        var orgIdClaim = context.HttpContext.User.FindFirst("OrganizationId");
        if (orgIdClaim == null || !Guid.TryParse(orgIdClaim.Value, out var organizationId))
        {
            return;
        }

        var quotaService = context.HttpContext.RequestServices.GetRequiredService<IQuotaService>();
        var canIngest = await quotaService.CanIngestReportAsync(organizationId, ct);
        if (!canIngest)
        {
            await context.HttpContext.Response.SendAsync(
                new { message = "Monthly report ingestion limit reached. Please upgrade your plan." },
                statusCode: 402, // Payment Required
                cancellation: ct);
        }
    }
}
