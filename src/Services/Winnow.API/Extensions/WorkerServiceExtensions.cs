using Npgsql;
using Amazon;
using Amazon.Comprehend;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleEmail;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Winnow.API.Infrastructure.Analysis;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Infrastructure.Integrations;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;
using Winnow.API.Services.Ai.Strategies;
using Winnow.API.Services.Storage;
using Winnow.API.Features.Dashboard.IService;
using Winnow.API.Features.Dashboard.Service;
using Winnow.API.Features.Clusters.GenerateSummary;
using Winnow.API.Domain.Services; // For IVectorCalculator
using Amazon.S3;
namespace Winnow.API.Extensions;

public static class WorkerServiceExtensions
{
    public static IServiceCollection AddWinnowBaseInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Multi-tenancy context (required for resolving DB connection string)
        services.AddScoped<ITenantContext, TenantContext>();

        // In-memory cache (required by NegativeMatchCache, etc.)
        services.AddMemoryCache();

        // LLM Configuration
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);
        services.AddSingleton(llmSettings);

        // Storage (S3/MinIO)
        var s3Settings = new S3Settings();
        config.GetSection("S3Settings").Bind(s3Settings);
        services.AddSingleton(s3Settings);

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var s3Config = new AmazonS3Config
            {
                ServiceURL = s3Settings.Endpoint,
                ForcePathStyle = true,
                UseHttp = s3Settings.Endpoint?.StartsWith("http://") ?? false
            };
            return new AmazonS3Client(s3Settings.AccessKey, s3Settings.SecretKey, s3Config);
        });
        services.AddSingleton<IStorageService, S3StorageService>();

        // Database
        services.AddScoped<DomainEventInterceptor>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(WinnowDbContext).Assembly);
        });

        services.AddDbContext<WinnowDbContext>((sp, options) =>
        {
            var tenantCtx = sp.GetRequiredService<ITenantContext>();
            var connStr = tenantCtx.ConnectionString;

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
            dataSourceBuilder.UseVector();
            var dataSource = dataSourceBuilder.Build();

            options.UseNpgsql(dataSource,
                npgsql =>
                {
                    npgsql.UseVector();
                    npgsql.MigrationsAssembly("Winnow.API");
                });
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
        });

        return services;
    }

    public static IServiceCollection AddWinnowSanitizeInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);

        services.AddHttpClient<LocalPiiRedactionProvider>().AddStandardResilienceHandler();
        services.AddSingleton<LocalPiiRedactionProvider>();
        services.AddSingleton<IPiiRedactionProvider>(sp => sp.GetRequiredService<LocalPiiRedactionProvider>());

        if (llmSettings.PiiRedactionProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddAWSService<IAmazonComprehend>();
            services.AddSingleton<IPiiRedactionProvider, AwsComprehendPiiRedactionProvider>();
        }

        services.AddSingleton<IPiiRedactionService, PiiRedactionService>();
        services.AddSingleton<IToxicityDetectionProvider, LocalToxicityDetectionProvider>();
        services.AddSingleton<IToxicityDetectionService, ToxicityDetectionService>();

        return services;
    }

    private static IServiceCollection AddWinnowKernel(this IServiceCollection services, IConfiguration config)
    {
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);

        if (llmSettings.Provider == "Ollama" && !string.IsNullOrEmpty(llmSettings.Ollama?.Endpoint))
        {
            services.AddKernel();

            // Default model
            services.AddOllamaChatCompletion(
                modelId: llmSettings.Ollama.ModelId,
                endpoint: new Uri(llmSettings.Ollama.Endpoint));

            // Specialized Gatekeeper model for duplicates
            services.AddOllamaChatCompletion(
                serviceId: "Gatekeeper",
                modelId: llmSettings.Ollama.GatekeeperModelId,
                endpoint: new Uri(llmSettings.Ollama.Endpoint));
        }
        else if (llmSettings.Provider == "OpenAI")
        {
            services.AddKernel();
            services.AddOpenAIChatCompletion(
                modelId: llmSettings.OpenAI.ModelId,
                apiKey: llmSettings.OpenAI.ApiKey);
        }

        return services;
    }

    public static IServiceCollection AddWinnowClusteringInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddWinnowKernel(config);

        // Register embedding providers as Singleton so ONNX models stay in memory
        services.AddSingleton<IEmbeddingProvider, OpenAiEmbeddingProvider>();
        services.AddSingleton<IEmbeddingProvider, LocalEmbeddingProvider>();
        try
        {
            services.AddSingleton<IEmbeddingProvider, OnnxEmbeddingProvider>();
        }
        catch (TypeInitializationException)
        {
            // Allowed to fail if ONNX runtime is completely missing
        }
        services.AddSingleton<IEmbeddingProvider, PlaceholderEmbeddingProvider>();

        // Register typed HTTP clients for embedding providers with resilience handlers
        services.AddHttpClient<OpenAiEmbeddingProvider>()
            .AddStandardResilienceHandler();

        services.AddHttpClient<LocalEmbeddingProvider>()
            .AddStandardResilienceHandler();

        services.AddSingleton<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<IVectorCalculator, VectorCalculator>();
        services.AddScoped<IDuplicateChecker, OllamaDuplicateChecker>();
        services.AddSingleton<INegativeMatchCache, NegativeMatchCache>();
        services.AddScoped<IClusterService, ClusterService>();
        return services;
    }

    public static IServiceCollection AddWinnowSummaryInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddWinnowKernel(config);
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);

        if (llmSettings.Provider == "Ollama" || llmSettings.Provider == "OpenAI")
        {
            services.AddScoped<IClusterSummaryService, SemanticKernelClusterSummaryService>();
        }
        else
        {
            services.AddScoped<IClusterSummaryService, PlaceholderSummaryService>();
        }

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ClusterSummaryOrchestrator>();
        return services;
    }

    // Convenience method
    public static IServiceCollection AddWinnowWorkerInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        return services
            .AddWinnowBaseInfrastructure(config)
            .AddWinnowSanitizeInfrastructure(config)
            .AddWinnowClusteringInfrastructure(config)
            .AddWinnowSummaryInfrastructure(config);
    }
}
