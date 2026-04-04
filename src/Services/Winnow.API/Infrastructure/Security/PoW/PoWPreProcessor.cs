using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Winnow.API.Infrastructure.Security.PoW;

public class PoWPreProcessor<TRequest>(
    IOptions<PoWSettings> settings,
    IPoWValidator validator) : IPreProcessor<TRequest>
{
    public async Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        var s = settings.Value;
        if (!s.Enabled) return;

        var httpContext = context.HttpContext;
        var request = httpContext.Request;

        // Extract headers
        var hasNonce = request.Headers.TryGetValue("X-Winnow-PoW-Nonce", out var nonce);
        var hasTimestamp = request.Headers.TryGetValue("X-Winnow-PoW-Timestamp", out var timestampStr);

        if (!hasNonce || !hasTimestamp || string.IsNullOrEmpty(nonce) || string.IsNullOrEmpty(timestampStr))
        {
            await FailAsync(httpContext, "Missing Proof-of-Work headers (X-Winnow-PoW-Nonce, X-Winnow-PoW-Timestamp).", ct);
            return;
        }

        // Validate timestamp
        if (!DateTimeOffset.TryParse(timestampStr, out var timestamp))
        {
            await FailAsync(httpContext, "Invalid Proof-of-Work timestamp format. Use ISO 8601.", ct);
            return;
        }

        var age = Math.Abs((DateTimeOffset.UtcNow - timestamp).TotalMinutes);
        if (age > s.MaxTimestampAgeMinutes)
        {
            await FailAsync(httpContext, $"Proof-of-Work timestamp is expired. It must be within {s.MaxTimestampAgeMinutes} minutes of current server time.", ct);
            return;
        }

        // Extract API key if present (used in the hash calculation as requested)
        request.Headers.TryGetValue("X-Winnow-Key", out var apiKey);

        // Verify PoW hash
        var isValid = validator.Verify(
            apiKey,
            request.Method,
            request.Path,
            timestampStr!,
            nonce!,
            s.Difficulty);

        if (!isValid)
        {
            await FailAsync(httpContext, $"Invalid Proof-of-Work solution for difficulty {s.Difficulty}.", ct);
            return;
        }

        // Replay protection: Check and mark nonce as used
        var isNewNonce = await validator.CheckAndMarkNonceUsedAsync(
            nonce!,
            TimeSpan.FromMinutes(s.MaxTimestampAgeMinutes));

        if (!isNewNonce)
        {
            await FailAsync(httpContext, "Proof-of-Work nonce has already been used.", ct);
        }
    }

    private static async Task FailAsync(HttpContext context, string message, CancellationToken ct)
    {
        // Short-circuit the request
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Proof-of-Work Validation Failed",
                Detail = message
            }, ct);
        }
    }
}
