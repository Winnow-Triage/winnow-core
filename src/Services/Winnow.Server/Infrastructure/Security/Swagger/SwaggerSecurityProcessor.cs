using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Winnow.Server.Infrastructure.Security.Swagger;

public class SwaggerSecurityProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var op = context.OperationDescription?.Operation;
        if (op == null) return true;

        // Ensure security requirements are initialized
        op.Security ??= [];

        // Enforce ApiKey scheme for Asset Status
        if (op.OperationId == "UpdateAssetStatus")
        {
            op.Security.Clear();
            op.Security.Add(new OpenApiSecurityRequirement
            {
                { "ApiKey", [] }
            });
        }
        // Enforce ProjectApiKey scheme for Ingest Report
        else if (op.OperationId == "IngestReport")
        {
            op.Security.Clear();
            op.Security.Add(new OpenApiSecurityRequirement
            {
                { "ProjectApiKey", [] }
            });
        }
        // Basic health endpoints should not require any authentication
        else if (op.OperationId == "HealthLive" || op.OperationId == "HealthReady" || op.OperationId == "Health")
        {
            // Clear security requirements for basic health endpoints
            op.Security.Clear();
        }
        // HealthDetailed endpoint requires SuperAdmin role (Bearer auth)
        else if (op.OperationId == "HealthDetailed")
        {
            // Keep Bearer auth requirement for detailed health endpoint
            op.Security.Clear();
            op.Security.Add(new OpenApiSecurityRequirement
            {
                { "Bearer", [] }
            });
        }
        else
        {
            // For all other endpoints, ensure we use Bearer auth
            op.Security.Clear();
            op.Security.Add(new OpenApiSecurityRequirement
            {
                { "Bearer", [] }
            });
        }

        return true;
    }
}
