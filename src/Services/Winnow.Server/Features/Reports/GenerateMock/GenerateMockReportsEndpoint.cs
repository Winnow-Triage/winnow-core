using System.Security.Claims;
using System.Text.Json;
using FastEndpoints;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.SemanticKernel;

using Winnow.Server.Features.Reports.Create;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.GenerateMock;

/// <summary>
/// Request to generate mock data.
/// </summary>
public class GenerateMockReportsRequest : ProjectScopedRequest
{
    /// <summary>
    /// Number of reports to generate.
    /// </summary>
    public int Count { get; set; } = 5;

    /// <summary>
    /// Contextual scenario for AI generation.
    /// </summary>
    public string? Scenario { get; set; }
}

public sealed class GenerateMockReportsEndpoint(
    Kernel kernel,
    WinnowDbContext db,
    IPublishEndpoint publishEndpoint,
    Services.Ai.IEmbeddingService embeddingService,
    Services.Quota.IQuotaService quotaService,
    ILogger<GenerateMockReportsEndpoint> logger) : ProjectScopedEndpoint<GenerateMockReportsRequest>
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public override void Configure()
    {
        Post("/reports/generate-mock");
        Summary(s =>
        {
            s.Summary = "Generate mock reports";
            s.Description = "Generates synthetic reports using AI for testing and demonstration purposes.";
            s.Response(200, "Mock reports generated successfully");
            s.Response(400, "Generation failed");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GenerateMockReportsRequest req, CancellationToken ct)
    {
        var prompt = $$"""
            Generate {{req.Count}} realistic technical support reports for a software application.
            Scenario context: {{req.Scenario ?? "General SaaS application issues"}}

            Each report must have a Title, User description.
            Return the result as a JSON array of objects with "title", "message" properties.
            IMPORTANT: String values MUST be on a single line. Use literal \n (backslash + n) for any newlines inside strings.
            
            Example output format:
            [
              { "title": "Example Issue", "message": "Example Issue"}
            ]
            """;

        var result = await kernel.InvokePromptAsync(prompt, cancellationToken: ct);
        var json = result.ToString();

        int firstBracket = json.IndexOf('[');
        int lastBracket = json.LastIndexOf(']');

        if (firstBracket != -1 && lastBracket != -1 && lastBracket > firstBracket)
        {
            json = json.Substring(firstBracket, lastBracket - firstBracket + 1);
        }

        try
        {
            var mockReports = JsonSerializer.Deserialize<List<MockReportDto>>(json, options) ?? throw new Exception("Failed to deserialize mock reports.");

            var tenantContext = db.GetService<ITenantContext>();
            var currentOrgId = tenantContext.CurrentOrganizationId ?? Guid.Empty;
            var tenantId = ((TenantContext)tenantContext).TenantId;

            foreach (var dt in mockReports)
            {
                var quotaStatus = await quotaService.GetIngestionQuotaStatusAsync(currentOrgId, ct);
                if (quotaStatus.isLocked)
                {
                    await quotaService.EnforceRetroactiveRansomAsync(currentOrgId, ct);
                }

                // Generate embedding for the mock report
                var textToEmbed = $"{dt.Title}\n{dt.Message}";
                var embeddingFloats = await embeddingService.GetEmbeddingAsync(textToEmbed);

                var report = new Domain.Reports.Report(
                    req.CurrentProjectId,
                    currentOrgId,
                    dt.Title,
                    dt.Message,
                    null,
                    null,
                    embeddingFloats,
                    null,
                    quotaStatus.isOverage,
                    quotaStatus.isLocked
                );

                db.Reports.Add(report);
                await db.SaveChangesAsync(ct);

                await publishEndpoint.Publish(new ReportCreatedEvent
                {
                    ReportId = report.Id,
                    Title = report.Title,
                    Message = report.Message,
                    CreatedAt = report.CreatedAt,
                    ProjectId = req.CurrentProjectId,
                    CurrentOrganizationId = Guid.TryParse(tenantId, out var orgId) ? orgId : throw new Exception("Invalid Tenant ID format")
                }, ct);
            }

            await Send.OkAsync(new { Message = $"Generated {mockReports.Count} reports with embeddings." }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GenerateMockReportsEndpoint. JSON was: {Json}", json);
            ThrowError($"Failed to generate mock reports: {ex.Message}", 400);
        }
    }

    public record MockReportDto(string Title, string Message);
}
