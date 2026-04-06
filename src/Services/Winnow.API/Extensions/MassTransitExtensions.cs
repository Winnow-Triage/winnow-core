using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddWinnowMassTransit(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment env,
        Action<IBusRegistrationConfigurator>? configureBus = null,
        Action<IBusRegistrationContext, IBusFactoryConfigurator>? configureFactory = null,
        bool enableOutbox = false)
    {
        services.AddMassTransit(x =>
        {
            configureBus?.Invoke(x);

            if (enableOutbox)
            {
                x.AddEntityFrameworkOutbox<WinnowDbContext>(o =>
                {
                    o.UsePostgres();
                    o.UseBusOutbox();
                });
            }

            // Selection Logic: Use InMemory if in Testing environment
            if (env.IsEnvironment("Testing") || config["USE_IN_MEMORY_TRANSPORT"] == "true")
            {
                x.UsingInMemory((context, cfg) =>
                {
                    configureFactory?.Invoke(context, cfg);
                    cfg.ConfigureEndpoints(context);
                });
            }
            else if (Environment.GetEnvironmentVariable("MESSAGE_BROKER")?.Equals("AmazonSqs", StringComparison.OrdinalIgnoreCase) == true)
            {
                x.UsingAmazonSqs((context, cfg) =>
                {
                    var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-2";
                    cfg.Host(region, h =>
                    {
                        // Will automatically use IAM execution roles attached to the ECS Task
                    });

                    configureFactory?.Invoke(context, cfg);
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    configureFactory?.Invoke(context, cfg);
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}
