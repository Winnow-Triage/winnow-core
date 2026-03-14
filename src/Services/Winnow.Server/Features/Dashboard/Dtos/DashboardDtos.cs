namespace Winnow.Server.Features.Dashboard.Dtos;

public record DashboardMetricsDto(
    TriageMetricsDto Triage,
    IReadOnlyCollection<TrendingClusterDto> TrendingClusters,
    IReadOnlyCollection<VolumeMetricDto> VolumeHistory);

public record TriageMetricsDto(
    int TotalReports,
    int ActiveClusters,
    double NoiseReductionRatio,
    int PendingReviews,
    int EstimatedHoursSaved);

public record TrendingClusterDto(
    Guid ClusterId,
    string Title,
    string Status,
    int ReportCount,
    int Velocity, // New reports in last X hours
    bool IsHot);

public record VolumeMetricDto(
    DateTime Timestamp,
    int NewUniqueCount,
    int DuplicateCount);

// Organization Dashboard DTOs
public record OrganizationDashboardDto(
    QuotaStatusDto Quota,
    IReadOnlyCollection<TeamBreakdownDto> TeamBreakdown,
    IReadOnlyCollection<TopProjectDto> TopProjects);

public record QuotaStatusDto(
    int TotalUsage,
    int? BaseLimit,
    int? GraceLimit,
    bool IsOverage,
    IReadOnlyCollection<MonthlyUsageDto> UsageHistory);

public record MonthlyUsageDto(
    string Month,
    int ReportCount,
    int ClusterCount);

public record TeamBreakdownDto(
    Guid TeamId,
    string TeamName,
    int ProjectCount,
    int ReportVolume);

public record TopProjectDto(
    Guid ProjectId,
    string ProjectName,
    int ReportCount,
    int ActiveClusters);

// Team Dashboard DTOs
public record TeamDashboardDto(
    IReadOnlyCollection<ProjectBreakdownDto> ProjectBreakdown,
    IReadOnlyCollection<TrendingClusterDto> TopClusters,
    IReadOnlyCollection<VolumeMetricDto> VolumeHistory);

public record ProjectBreakdownDto(
    Guid ProjectId,
    string ProjectName,
    int ReportVolume,
    int ActiveClusters);
