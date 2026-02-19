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
