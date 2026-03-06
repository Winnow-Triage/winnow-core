using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Winnow.Server.Infrastructure.HealthChecks;

/// <summary>
/// DelegatingHandler that intercepts all requests going through the
/// "ExternalIntegrations" named HttpClient and updates
/// <see cref="ExternalIntegrationHealthTracker"/> with success/failure state.
/// </summary>
public sealed class ExternalIntegrationTrackerHandler : DelegatingHandler
{
    private readonly ExternalIntegrationHealthTracker _tracker;

    public ExternalIntegrationTrackerHandler(ExternalIntegrationHealthTracker tracker)
    {
        _tracker = tracker;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _tracker.RecordSuccess();
            }
            else
            {
                _tracker.RecordFailure($"HTTP {(int)response.StatusCode} from {request.RequestUri?.Host}");
            }

            return response;
        }
        catch (Exception ex)
        {
            _tracker.RecordFailure($"{ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}
