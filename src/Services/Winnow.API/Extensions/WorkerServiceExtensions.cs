using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai.Strategies;
using Npgsql;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Services.Ai;
using Winnow.API.Features.Dashboard.Service;
using Winnow.API.Features.Dashboard.IService;
using Winnow.API.Infrastructure.Analysis;
using Winnow.API.Domain.Services;
using Winnow.API.Features.Clusters.GenerateSummary;

namespace Winnow.API.Extensions;

public static class WorkerServiceExtensions
{
    public static IServiceCollection AddWinnowBaseInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddInfrastructureServices(config);
        services.AddWorkerMessaging();
        services.AddEmailAndNotifications(config);

        return services;
    }

    private static void AddWorkerMessaging(this IServiceCollection services)
    {
        if (Environment.GetEnvironmentVariable("MESSAGE_BROKER")?.Equals("AmazonSqs", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddAWSService<Amazon.SQS.IAmazonSQS>();
            services.AddAWSService<Amazon.SimpleNotificationService.IAmazonSimpleNotificationService>();
        }
    }

    public static IServiceCollection AddWinnowSanitizeInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);

        services.AddHttpClient<LocalPiiRedactionProvider>().AddStandardResilienceHandler();
        services.AddSingleton<IPiiRedactionProvider>(sp => sp.GetRequiredService<LocalPiiRedactionProvider>());

        if (llmSettings.PiiRedactionProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddAWSService<Amazon.Comprehend.IAmazonComprehend>();
            services.AddSingleton<IPiiRedactionProvider, AwsComprehendPiiRedactionProvider>();
        }

        services.AddSingleton<IPiiRedactionService, PiiRedactionService>();
        services.AddSingleton<IToxicityDetectionProvider, LocalToxicityDetectionProvider>();
        services.AddSingleton<IToxicityDetectionService, ToxicityDetectionService>();

        return services;
    }

    public static IServiceCollection AddWinnowClusteringInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);

        services.AddWinnowKernel(llmSettings);

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
        services.AddHttpClient<OpenAiEmbeddingProvider>().AddStandardResilienceHandler();
        services.AddHttpClient<LocalEmbeddingProvider>().AddStandardResilienceHandler();

        services.AddSingleton<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<IVectorCalculator, VectorCalculator>();

        if (llmSettings.Provider == "Ollama")
        {
            services.AddScoped<IDuplicateChecker, OllamaDuplicateChecker>();
        }
        else
        {
            services.AddScoped<IDuplicateChecker, PlaceholderDuplicateChecker>();
        }

        services.AddSingleton<INegativeMatchCache, NegativeMatchCache>();
        services.AddScoped<IClusterService, ClusterService>();

        return services;
    }

    public static IServiceCollection AddWinnowSummaryInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);

        services.AddWinnowKernel(llmSettings);

        if (llmSettings.Provider == "Ollama" || llmSettings.Provider == "OpenAI" || llmSettings.Provider == "Bedrock")
        {
            services.AddScoped<IClusterSummaryService, SemanticKernelClusterSummaryService>();
        }
        else
        {
            services.AddScoped<IClusterSummaryService, PlaceholderClusterSummaryService>();
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
