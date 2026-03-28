using FastEndpoints;

namespace Winnow.API.Infrastructure.Security;

public class ApiKeyAuthPreProcessor<TRequest>(IConfiguration config) : IPreProcessor<TRequest>
{
    public async Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        var apiKey = config["Bouncer:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            // Fail safe: if no key is configured, deny everything (or log error)
            await context.HttpContext.Response.SendUnauthorizedAsync(ct);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey) ||
            !string.Equals(extractedApiKey, apiKey, StringComparison.Ordinal))
        {
            await context.HttpContext.Response.SendUnauthorizedAsync(ct);
        }
    }
}
