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
                config.EnsureRdsSslCertificate();

                // Wolverine uses Weasel to manage schema for outbox.
                // We MUST use a distinct schema for each application to prevent node control overlaps and timeouts
                var schemaName = $"wolverine_{env.ApplicationName.Replace(".", "_").ToLowerInvariant()}";
                opts.PersistMessagesWithPostgresql(connString, schemaName);
                opts.UseEntityFrameworkCoreTransactions();

                // Ensures Wolverine creates envelope and node tables idempotently on startup.
                // This is required because Wolverine tables are not mapped in the EF DbContext model.
                opts.AutoBuildMessageStorageOnStartup = JasperFx.AutoCreate.CreateOrUpdate;

                // Enforce durability agent on sender-only applications so the Outbox is actually swept
                opts.Durability.Mode = DurabilityMode.Balanced;
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
                var projectName = config["ProjectName"] ?? "winnow";

                opts.UseRabbitMq(x =>
                {
                    x.HostName = rmqHost;
                    x.UserName = "guest";
                    x.Password = "guest";
                }).AutoProvision();

                opts.PublishMessage<GenerateClusterSummaryEvent>().ToRabbitQueue($"{projectName}-summary-queue");
                opts.PublishMessage<ReportCreatedEvent>().ToRabbitQueue($"{projectName}-sanitize-queue");
                opts.PublishMessage<ReportSanitizedEvent>().ToRabbitQueue($"{projectName}-clustering-queue");
                opts.PublishMessage<SendWebhookNotificationCommand>().ToRabbitQueue($"{projectName}-notifications-queue");
            }
        });

        return hostBuilder;
    }
}
