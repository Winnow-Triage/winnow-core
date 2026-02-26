namespace Winnow.Server.Services.Quota;

/// <summary>
/// Service for checking and enforcing quotas.
/// </summary>
public interface IQuotaService
{
    /// <summary>
    /// Checks if an organization can ingest a report based on their current quota.
    /// </summary>
    /// <param name="organizationId">The ID of the organization.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the organization can ingest a report, <c>false</c> otherwise.</returns>
    Task<bool> CanIngestReportAsync(Guid organizationId, CancellationToken ct = default);
}
