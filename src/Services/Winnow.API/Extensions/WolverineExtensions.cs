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
                // Or you can explicitly route to local queues if desired
            }
            else if (Environment.GetEnvironmentVariable("MESSAGE_BROKER")?.Equals("AmazonSqs", StringComparison.OrdinalIgnoreCase) == true)
            {
                var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-2";

                opts.UseAmazonSqsTransport(sqs =>
                {
                    // By default, it will use standard AWS SDK credential chains
                    sqs.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
                });
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
