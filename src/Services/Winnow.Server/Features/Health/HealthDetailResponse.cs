using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Winnow.Server.Features.Health;

public sealed record HealthDetailResponse
{
    public required string Status { get; init; }
    public required string TotalDuration { get; init; }
    public required string UtcTimestamp { get; init; }
    public List<HealthCheckDetail> Checks { get; init; } = [];
    public required CircuitBreakersInfo CircuitBreakers { get; init; }

    public sealed record HealthCheckDetail
    {
        internal HealthCheckDetail() {} // Internal constructor prevents direct instantiation
        public required string Name { get; init; }
        public required string Status { get; init; }
        public required string Duration { get; init; }
        public string? Description { get; init; }
        public required IReadOnlyDictionary<string, object> Data { get; init; }
        public string? Exception { get; init; }
        public required IEnumerable<string> Tags { get; init; }
    }

    public sealed record CircuitBreakersInfo
    {
        internal CircuitBreakersInfo() {} // Internal constructor prevents direct instantiation
        public required string ExternalIntegrations { get; init; }
        public required string HttpClients { get; init; }
    }

    public static HealthDetailResponse FromHealthReport(HealthReport report)
    {
        return new HealthDetailResponse
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration.ToString(),
            UtcTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Checks = report.Entries.Select(entry => new HealthCheckDetail
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Duration = entry.Value.Duration.ToString(),
                Description = entry.Value.Description,
                Data = entry.Value.Data,
                Exception = entry.Value.Exception?.Message,
                Tags = entry.Value.Tags
            }).ToList(),
            CircuitBreakers = new CircuitBreakersInfo
            {
                ExternalIntegrations = "Configured",
                HttpClients = "With resilience pipeline"
            }
        };
    }
}