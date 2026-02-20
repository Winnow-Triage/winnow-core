using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Winnow.Server.Infrastructure.HealthChecks;

public static class HealthCheckJsonWriter
{
    public static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        
        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            utcTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.ToString(),
                description = entry.Value.Description,
                data = entry.Value.Data,
                exception = entry.Value.Exception?.Message,
                tags = entry.Value.Tags
            }),
            circuitBreakers = new
            {
                // Circuit breaker status could be added here by querying resilience pipeline metrics
                externalIntegrations = "Configured",
                httpClients = "With resilience pipeline"
            }
        };
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}