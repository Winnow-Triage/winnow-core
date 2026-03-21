namespace Winnow.API.Services.Quota;

/// <summary>
/// Service for checking and enforcing quotas.
/// </summary>
public interface IQuotaService
{
    /// <summary>
    /// Gets the quota status for a new report ingestion.
    /// Evaluates base limits and grace limits to determine if the report should be marked as overage or locked.
    /// </summary>
    /// <param name="organizationId">The ID of the organization.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple indicating whether the report is over the base limit (isOverage) and over the grace limit (isLocked).</returns>
    Task<(bool isOverage, bool isLocked)> GetIngestionQuotaStatusAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Retroactively locks all overage reports for the current month when the grace limit is exceeded.
    /// </summary>
    /// <param name="organizationId">The ID of the organization.</param>
    /// <param name="ct">Cancellation token.</param>
    Task EnforceRetroactiveRansomAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Resolves quota discrepancies when an organization's subscription tier changes.
    /// This will automatically unlock reports if a user upgrades their plan, or lock them if they downgrade.
    /// </summary>
    /// <param name="organizationId">The ID of the organization.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ResolveQuotaDiscrepanciesAsync(Guid organizationId, CancellationToken ct = default);
}
