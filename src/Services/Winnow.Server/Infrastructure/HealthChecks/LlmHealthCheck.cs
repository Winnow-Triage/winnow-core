using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Infrastructure.HealthChecks;

public class LlmHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LlmSettings _llmSettings;

    public LlmHealthCheck(IHttpClientFactory httpClientFactory, LlmSettings llmSettings)
    {
        _httpClientFactory = httpClientFactory;
        _llmSettings = llmSettings;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["Provider"] = _llmSettings.Provider
        };

        if (_llmSettings.Provider == "Ollama")
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ExternalIntegrations");
                client.Timeout = TimeSpan.FromSeconds(5);

                var endpoint = _llmSettings.Ollama.Endpoint.TrimEnd('/');
                var response = await client.GetAsync($"{endpoint}/api/tags", cancellationToken);

                data["Endpoint"] = endpoint;
                data["Model"] = _llmSettings.Ollama.ModelId;
                data["GatekeeperModel"] = _llmSettings.Ollama.GatekeeperModelId;

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Ollama reachable", data);
                }

                data["StatusCode"] = (int)response.StatusCode;
                return HealthCheckResult.Unhealthy(
                    $"Ollama returned {(int)response.StatusCode} {response.StatusCode}",
                    data: data);
            }
            catch (OperationCanceledException)
            {
                data["Error"] = "Request timed out after 5 s";
                return HealthCheckResult.Unhealthy("Ollama unreachable (timeout)", data: data);
            }
            catch (Exception ex)
            {
                data["Error"] = ex.Message;
                return HealthCheckResult.Unhealthy("Ollama unreachable", data: data);
            }
        }

        if (_llmSettings.Provider == "OpenAI")
        {
            data["Model"] = _llmSettings.OpenAI.ModelId;
            data["Note"] = "Live ping skipped to avoid paid API calls";
            return HealthCheckResult.Healthy("OpenAI configured", data);
        }

        // Placeholder / no LLM configured
        data["Note"] = "No LLM provider configured — AI summary features are disabled";
        return HealthCheckResult.Healthy("Not configured", data);
    }
}
