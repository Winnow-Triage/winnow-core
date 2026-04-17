using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Persistence.Repositories;
using Winnow.API.Infrastructure.Integrations;
using Winnow.API.Infrastructure.Integrations.Strategies;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using FastEndpoints;

namespace Winnow.API.Extensions;

internal static class PipelineExtensions
{
    public static IServiceCollection AddMiddlewareAndPolicies(this IServiceCollection services, IConfiguration config)
    {
        // Assembly scanning for strategy implementations
        services.Scan(scan => scan
            .FromAssemblyOf<IExporterCreationStrategy>()
            .AddClasses(classes => classes.AssignableTo<IExporterCreationStrategy>())
            .As<IExporterCreationStrategy>()
            .WithScopedLifetime()
        );

        services.Scan(scan => scan
            .FromAssemblyOf<IIntegrationConfigDeserializationStrategy>()
            .AddClasses(classes => classes.AssignableTo<IIntegrationConfigDeserializationStrategy>())
            .As<IIntegrationConfigDeserializationStrategy>()
            .WithScopedLifetime()
        );

        // Core HTTP & resilience
        services.AddWinnowHttpClients();

        // AWS Shared
        services.AddDefaultAWSOptions(config.GetAWSOptions());

        // FastEndpoints & Swagger
        services.AddFastEndpoints();
        services.AddWinnowOpenApi();

        // Middleware Config
        services.AddWinnowHeadersConfig();

        // Rate Limiting
        services.AddWinnowRateLimiting();

        // CORS
        services.AddWinnowCors();

        // MediatR
        services.AddWinnowMediatR();

        services.AddScoped<DomainEventInterceptor>();

        return services;
    }

    private static void AddWinnowHttpClients(this IServiceCollection services)
    {
        services.AddSingleton<ExternalIntegrationHealthTracker>();
        services.AddTransient<ExternalIntegrationTrackerHandler>();
        services.AddHttpClient("ExternalIntegrations")
            .RemoveAllLoggers()
            .AddHttpMessageHandler<ExternalIntegrationTrackerHandler>()
            .AddStandardResilienceHandler();

        services.AddHttpClient<Winnow.API.Services.Ai.Strategies.OpenAiEmbeddingProvider>().AddStandardResilienceHandler();
        services.AddHttpClient<Winnow.API.Services.Ai.Strategies.LocalEmbeddingProvider>().AddStandardResilienceHandler();
        services.AddHttpClient<Winnow.API.Infrastructure.Analysis.LocalPiiRedactionProvider>().AddStandardResilienceHandler();
        services.AddHttpClient();
    }

    private static void AddWinnowHeadersConfig(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = null;
        });
    }

    private static void AddWinnowCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://app.winnowtriage.com", "https://winnowtriage.com", "https://www.winnowtriage.com", "http://localhost:5173", "http://localhost:5174")
                      .AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetPreflightMaxAge(TimeSpan.FromHours(2));
            });
        });
    }

    private static void AddWinnowMediatR(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Program>();
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        });
    }

    private static void AddWinnowOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi();
    }

    private static void AddWinnowRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;

            options.AddFixedWindowLimiter("strict", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 10;
                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
            options.AddFixedWindowLimiter("api", opt =>
            {
                opt.Window = TimeSpan.FromSeconds(1);
                opt.PermitLimit = 100;
                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
            options.AddPolicy("webhook", context =>
            {
                var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                         ?? context.Connection.RemoteIpAddress?.ToString()
                         ?? "unknown";

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                    new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(10),
                        PermitLimit = 5,
                        QueueLimit = 0,
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
                    });
            });
            options.AddFixedWindowLimiter("email_dispatch", opt =>
            {
                opt.Window = TimeSpan.FromSeconds(5);
                opt.PermitLimit = 5;
                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });
    }

}
