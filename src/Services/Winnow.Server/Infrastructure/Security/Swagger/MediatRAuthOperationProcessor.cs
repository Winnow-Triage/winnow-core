using System.Reflection;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Infrastructure.Security.Swagger;

public class MediatRAuthOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var op = context.OperationDescription?.Operation;
        if (op == null) return true;

        var methodInfo = context.MethodInfo;
        if (methodInfo == null) return true;

        var requiredPermissions = new List<string>();

        // Check parameters of the Handle method (the incoming DTOs/Requests)
        foreach (var parameter in methodInfo.GetParameters())
        {
            var attrs = parameter.ParameterType.GetCustomAttributes<RequirePermissionAttribute>(true);
            requiredPermissions.AddRange(attrs.Select(a => a.Permission));
        }

        // Also check the Endpoint class itself in case the attribute is placed there
        if (methodInfo.DeclaringType != null)
        {
            var endpointAttrs = methodInfo.DeclaringType.GetCustomAttributes<RequirePermissionAttribute>(true);
            requiredPermissions.AddRange(endpointAttrs.Select(a => a.Permission));
        }

        if (requiredPermissions.Count > 0)
        {
            if (!op.Responses.ContainsKey("401"))
            {
                op.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            }

            if (!op.Responses.ContainsKey("403"))
            {
                op.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
            }

            var permissionsText = string.Join(", ", requiredPermissions.Distinct());
            var note = $"<br/>**Requires Permission(s):** {permissionsText}";

            if (string.IsNullOrEmpty(op.Description))
            {
                op.Description = note;
            }
            else if (!op.Description.Contains("Requires Permission(s):"))
            {
                op.Description += $"\n\n{note}";
            }
        }

        return true;
    }
}
