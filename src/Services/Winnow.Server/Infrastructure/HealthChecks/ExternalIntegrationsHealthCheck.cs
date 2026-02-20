using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Infrastructure.HealthChecks;

public class ExternalIntegrationsHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LlmSettings _llmSettings;

    public ExternalIntegrationsHealthCheck(IHttpClientFactory httpClientFactory, LlmSettings llmSettings)
    {
        _httpClientFactory = httpClientFactory;
        _llmSettings = llmSettings;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var failures = new List<string>();

        // Check Ollama if configured
        if (_llmSettings.Provider == "Ollama")
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ExternalIntegrations");
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync($"{_llmSettings.Ollama.Endpoint}/api/tags", cancellationToken);
                data["Ollama"] = response.IsSuccessStatusCode ? "Healthy" : $"Unhealthy (Status: {response.StatusCode})";

                if (!response.IsSuccessStatusCode)
                {
                    failures.Add($"Ollama: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                data["Ollama"] = $"Unhealthy: {ex.Message}";
                failures.Add($"Ollama: {ex.Message}");
            }
        }
        else if (_llmSettings.Provider == "OpenAI")
        {
            // For OpenAI, we could attempt a simple health check, but API calls cost money
            // Instead, we'll just report configuration status
            data["OpenAI"] = "Configured";
        }

        // Check external integrations HTTP client
        try
        {
            var externalClient = _httpClientFactory.CreateClient("ExternalIntegrations");
            data["ExternalIntegrationsHttpClient"] = "Configured with resilience pipeline";
        }
        catch (Exception ex)
        {
            data["ExternalIntegrationsHttpClient"] = $"Configuration error: {ex.Message}";
            failures.Add($"ExternalIntegrationsHttpClient: {ex.Message}");
        }

        if (failures.Count == 0)
        {
            return HealthCheckResult.Healthy(
                "Healthy",
                data);
        }

        return HealthCheckResult.Unhealthy(
            $"External integrations have {failures.Count} issue(s)",
            data: data);
    }
}