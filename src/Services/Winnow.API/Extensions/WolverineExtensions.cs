using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.AmazonSqs;
using Wolverine.RabbitMQ;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Winnow.API.Infrastructure.Persistence;
using Amazon;
using Winnow.Contracts;

namespace Winnow.API.Extensions;

public static class WolverineExtensions
{
    public static IHostBuilder UseWinnowWolverine(
        this IHostBuilder hostBuilder,
        IConfiguration config,
        IHostEnvironment env,
        Action<WolverineOptions>? configure = null,
        bool enableOutbox = false)
    {
        hostBuilder.UseWolverine(opts =>
        {
            configure?.Invoke(opts);

            if (enableOutbox)
            {
                var connString = config.GetConnectionString("Postgres")
                    ?? throw new InvalidOperationException("Postgres connection string missing.");

                // Bridge for AWS-managed passwords or GitHub secrets
                var dbPassword = config["DB_PASSWORD"];
                if (!string.IsNullOrEmpty(dbPassword) && connString.Contains("{password}"))
                {
                    connString = connString.Replace("{password}", dbPassword);
                }

                // Bridge for SSL Certificate Download (Optional/Portability)
                var certUrl = config["DB_SSL_CERT_URL"];
                if (!string.IsNullOrEmpty(certUrl) && connString.Contains("Root Certificate="))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(connString, @"Root Certificate=([^;]+)");
                    if (match.Success)
                    {
                        var certPath = match.Groups[1].Value.Trim();
                        if (!System.IO.File.Exists(certPath))
                        {
                            try
                            {
                                using var client = new System.Net.Http.HttpClient();
                                var certData = client.GetByteArrayAsync(certUrl).GetAwaiter().GetResult();
                                var directory = System.IO.Path.GetDirectoryName(certPath);
                                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                                    System.IO.Directory.CreateDirectory(directory);
                                System.IO.File.WriteAllBytes(certPath, certData);
                            }
                            catch { /* Fallback to standard connection if download fails */ }
                        }
                    }
                }

                // Wolverine uses Weasel to manage schema for outbox
                opts.PersistMessagesWithPostgresql(connString);
                opts.UseEntityFrameworkCoreTransactions();

                // Ensures Wolverine creates envelope and node tables idempotently on startup.
                // This is required because Wolverine tables are not mapped in the EF DbContext model.
                opts.AutoBuildMessageStorageOnStartup = JasperFx.AutoCreate.CreateOrUpdate;
            }

            if (env.IsEnvironment("Testing") || config["USE_IN_MEMORY_TRANSPORT"] == "true")
            {
                // Wolverine handles local/in-memory routing out of the box when no external broker is configured
            }
            else if (Environment.GetEnvironmentVariable("MESSAGE_BROKER")?.Equals("AmazonSqs", StringComparison.OrdinalIgnoreCase) == true)
            {
                var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-2";
                var projectName = config["ProjectName"] ?? "winnow";

                opts.UseAmazonSqsTransport(sqs =>
                {
                    sqs.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
                }).DisableAllNativeDeadLetterQueues();

                // Routing to SQS Queues
                opts.PublishMessage<GenerateClusterSummaryEvent>().ToSqsQueue($"{projectName}-summary-queue");
                opts.PublishMessage<ReportCreatedEvent>().ToSqsQueue($"{projectName}-sanitize-queue");
                opts.PublishMessage<ReportSanitizedEvent>().ToSqsQueue($"{projectName}-clustering-queue");
                opts.PublishMessage<SendWebhookNotificationCommand>().ToSqsQueue($"{projectName}-notifications-queue");
            }
            else
            {
                var rmqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
                opts.UseRabbitMq(x =>
                {
                    x.HostName = rmqHost;
                    x.UserName = "guest";
                    x.Password = "guest";
                });
            }
        });

        return hostBuilder;
    }
}
