namespace Winnow.Server.Features.Dashboard;

public record DashboardMetricsDto(
    TriageMetricsDto Triage,
    List<TrendingClusterDto> TrendingClusters,
    List<VolumeMetricDto> VolumeHistory);

public record TriageMetricsDto(
    int TotalTickets,
    int ActiveClusters,
    double NoiseReductionRatio,
    int PendingReviews,
    int EstimatedHoursSaved);

public record TrendingClusterDto(
    Guid ClusterId,
    string Title,
    string Status,
    int TicketCount,
    int Velocity, // New tickets in last X hours
    bool IsHot);

public record VolumeMetricDto(
    DateTime Timestamp,
    int NewUniqueCount,
    int DuplicateCount);
