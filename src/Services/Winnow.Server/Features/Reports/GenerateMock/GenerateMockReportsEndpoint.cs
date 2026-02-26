using System.Security.Claims;
using System.Text.Json;
using FastEndpoints;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.SemanticKernel;
using Winnow.Server.Entities;
using Winnow.Server.Features.Reports.Create;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.GenerateMock;

/// <summary>
/// Request to generate mock data.
/// </summary>
public class GenerateMockReportsRequest
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
    Winnow.Server.Services.Ai.IEmbeddingService embeddingService,
    Winnow.Server.Services.Quota.IQuotaService quotaService,
    ILogger<GenerateMockReportsEndpoint> logger) : Endpoint<GenerateMockReportsRequest>
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
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        // Get project ID from header
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        // Validate user owns this project
        var userOwnsProject = await db.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);

        if (!userOwnsProject)
        {
            ThrowError("Project not found or access denied", 404);
        }

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
                var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
                Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

                var report = new Report
                {
                    Title = dt.Title,
                    Message = dt.Message,
                    Status = "New",
                    CreatedAt = DateTime.UtcNow,
                    ProjectId = projectId, // Set the project ID
                    OrganizationId = currentOrgId,
                    Embedding = embeddingBytes, // Include the embedding
                    IsOverage = quotaStatus.isOverage,
                    IsLocked = quotaStatus.isLocked
                };

                db.Reports.Add(report);
                await db.SaveChangesAsync(ct);

                await publishEndpoint.Publish(new ReportCreatedEvent
                {
                    ReportId = report.Id,
                    Title = report.Title,
                    Message = report.Message,
                    CreatedAt = report.CreatedAt,
                    ProjectId = projectId, // Use the actual project ID
                    TenantId = tenantId
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
