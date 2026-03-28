using System.Security.Claims;
using System.Text.Json;
using FastEndpoints;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

using Winnow.API.Features.Reports.Create;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Reports.GenerateMock;

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
    IMediator mediator,
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

        // Check if any chat completion service is available in the kernel
        var chatService = kernel.Services.GetService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
        if (chatService == null)
        {
            logger.LogWarning("GenerateMockReportsEndpoint called but no chat completion service is registered (Provider: {Provider})", "None");
            await Send.ResponseAsync(new { Error = "Mock report generation requires an active AI provider (Ollama or OpenAI). Current provider is 'None'." }, StatusCodes.Status400BadRequest, cancellation: ct);
            return;
        }

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

            foreach (var dt in mockReports)
            {
                var command = new CreateReportCommand(
                    currentOrgId,
                    req.CurrentProjectId,
                    dt.Title,
                    dt.Message
                );

                await mediator.Send(command, ct);
            }

            await Send.OkAsync(new { Message = $"Generated {mockReports.Count} reports via MediatR pipeline." }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GenerateMockReportsEndpoint. JSON was: {Json}", json);
            ThrowError($"Failed to generate mock reports: {ex.Message}", 400);
        }
    }

    public record MockReportDto(string Title, string Message);
}
