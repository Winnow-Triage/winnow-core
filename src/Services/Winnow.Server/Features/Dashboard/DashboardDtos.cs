namespace Winnow.Server.Features.Dashboard;

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
