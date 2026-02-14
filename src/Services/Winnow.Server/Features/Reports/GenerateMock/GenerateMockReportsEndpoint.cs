using System.Text.Json;
using FastEndpoints;
using MassTransit;
using Microsoft.SemanticKernel;
using Winnow.Server.Entities;
using Winnow.Server.Features.Reports.Create;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.GenerateMock;

public class GenerateMockReportsRequest
{
    public int Count { get; set; } = 5;
    public string? Scenario { get; set; }
}

public class GenerateMockReportsEndpoint(
    Kernel kernel,
    WinnowDbContext db,
    IPublishEndpoint publishEndpoint,
    ITenantContext tenantContext,
    ILogger<GenerateMockReportsEndpoint> logger) : Endpoint<GenerateMockReportsRequest>
{
    public override void Configure()
    {
        Post("/reports/generate-mock");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GenerateMockReportsRequest req, CancellationToken ct)
    {
        var prompt = $$"""
            Generate {{req.Count}} realistic technical support reports for a software application.
            Scenario context: {{req.Scenario ?? "General SaaS application issues"}}

            Each report must have a Message and a StackTrace (or more details).
            Return the result as a JSON array of objects with "message" and "stackTrace" properties.
            IMPORTANT: String values MUST be on a single line. Use literal \n (backslash + n) for any newlines inside strings.
            
            Example output format:
            [
              { "message": "Example Issue", "stackTrace": "Line 1\\nLine 2" }
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
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };
            var mockReports = JsonSerializer.Deserialize<List<MockReportDto>>(json, options);

            if (mockReports == null)
            {
                throw new Exception("Failed to deserialize mock reports.");
            }

            foreach (var dt in mockReports)
            {
                var report = new Report
                {
                    Message = dt.Message,
                    StackTrace = dt.StackTrace,
                    Status = "New",
                    CreatedAt = DateTime.UtcNow
                };

                db.Reports.Add(report);
                await db.SaveChangesAsync(ct);

                await publishEndpoint.Publish(new ReportCreatedEvent
                {
                    ReportId = report.Id,
                    Message = report.Message,
                    StackTrace = report.StackTrace,
                    CreatedAt = report.CreatedAt,
                    ProjectId = Guid.Empty // Mock reports might not have a real project
                }, ct);
            }

            await Send.OkAsync(new { Message = $"Generated {mockReports.Count} reports." }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GenerateMockReportsEndpoint. JSON was: {Json}", json);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }

    private record MockReportDto(string Message, string StackTrace);
}
