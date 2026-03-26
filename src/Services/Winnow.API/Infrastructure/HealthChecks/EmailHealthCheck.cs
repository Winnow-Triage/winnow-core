using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Infrastructure.HealthChecks;

public class EmailHealthCheck : IHealthCheck
{
    private readonly EmailSettings _emailSettings;

    public EmailHealthCheck(EmailSettings emailSettings)
    {
        _emailSettings = emailSettings;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["Provider"] = _emailSettings.Provider,
            ["FromAddress"] = _emailSettings.FromAddress
        };

        if (string.IsNullOrEmpty(_emailSettings.Provider) ||
            _emailSettings.Provider.Equals("None", StringComparison.OrdinalIgnoreCase) ||
            _emailSettings.Provider.Equals("Placeholder", StringComparison.OrdinalIgnoreCase))
        {
            data["Note"] = "No email provider configured — email features are disabled";
            return HealthCheckResult.Healthy("Not configured", data);
        }

        if (_emailSettings.Provider == "AwsSes")
        {
            data["Region"] = _emailSettings.AwsSes.Region;
            data["Note"] = "Reachability monitored via AWS CloudWatch";
            return HealthCheckResult.Healthy("AWS SES configured", data);
        }

        if (_emailSettings.Provider != "Smtp" && _emailSettings.Provider != "SmtpClient")
        {
            data["Note"] = $"Unknown provider '{_emailSettings.Provider}' — defaulting to Healthy";
            return HealthCheckResult.Healthy("Unknown provider", data);
        }

        // SMTP — attempt a TCP connect to confirm the mail server is reachable
        var host = _emailSettings.Smtp.Host;
        var port = _emailSettings.Smtp.Port;
        data["Host"] = host;
        data["Port"] = port;
        data["Ssl"] = _emailSettings.Smtp.EnableSsl;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, cts.Token);
            return HealthCheckResult.Healthy("SMTP reachable", data);
        }
        catch (OperationCanceledException)
        {
            data["Error"] = $"TCP connection to {host}:{port} timed out after 3 s";
            return HealthCheckResult.Unhealthy("SMTP unreachable (timeout)", data: data);
        }
        catch (Exception ex)
        {
            data["Error"] = ex.Message;
            return HealthCheckResult.Unhealthy("SMTP unreachable", data: data);
        }
    }
}
