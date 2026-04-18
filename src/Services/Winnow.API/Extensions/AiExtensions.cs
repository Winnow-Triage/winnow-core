using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Services.Ai;
using Winnow.API.Services.Ai.Strategies;
using Winnow.API.Domain.Services;
using Winnow.API.Features.Clusters.GenerateSummary;
using Winnow.API.Features.Dashboard.Service;
using Winnow.API.Infrastructure.Analysis;
using Winnow.API.Features.Dashboard.IService;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.Amazon;

namespace Winnow.API.Extensions;

internal static class AiExtensions
{
    private const string AmazonComprehend = "AmazonComprehend";

    public static IServiceCollection AddAiAndLlmServices(this IServiceCollection services, IConfiguration config)
    {
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);

        // LLM Strategy scanning
        services.Scan(scan => scan
            .FromAssemblyOf<IEmbeddingProvider>()
            .AddClasses(classes => classes.AssignableTo<IEmbeddingProvider>())
            .As<IEmbeddingProvider>()
            .WithSingletonLifetime()
        );

        // Toxicity & PII
        services.AddToxicityAndPiiServices(llmSettings);

        // AI Core
        services.AddScoped<ClusterSummaryOrchestrator>();
        services.AddSingleton<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<IVectorCalculator, VectorCalculator>();

        // Semantic Kernel
        services.AddWinnowKernel(llmSettings);

        // Duplicate Checkers
        services.AddDuplicateCheckers(llmSettings);

        services.AddSingleton<INegativeMatchCache, NegativeMatchCache>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IClusterService, ClusterService>();

        return services;
    }

    private static void AddToxicityAndPiiServices(this IServiceCollection services, LlmSettings llmSettings)
    {
        services.AddSingleton<IToxicityDetectionProvider, LocalToxicityDetectionProvider>();
        services.AddSingleton<IToxicityDetectionService, ToxicityDetectionService>();

        services.AddSingleton<LocalPiiRedactionProvider>();
        services.AddSingleton<IPiiRedactionProvider>(sp => sp.GetRequiredService<LocalPiiRedactionProvider>());
        services.AddSingleton<IPiiRedactionService, PiiRedactionService>();

        if (llmSettings.ToxicityProvider == AmazonComprehend || llmSettings.PiiRedactionProvider == AmazonComprehend)
        {
            services.AddAWSService<Amazon.Comprehend.IAmazonComprehend>();
            if (llmSettings.ToxicityProvider == AmazonComprehend)
                services.AddSingleton<IToxicityDetectionProvider, AwsComprehendToxicityDetectionProvider>();
            if (llmSettings.PiiRedactionProvider == AmazonComprehend)
                services.AddSingleton<IPiiRedactionProvider, AwsComprehendPiiRedactionProvider>();
        }
    }

    public static void AddWinnowKernel(this IServiceCollection services, LlmSettings llmSettings)
    {
        var kernelBuilder = services.AddKernel();

        if (llmSettings.Provider == "Ollama")
        {
            kernelBuilder.AddOllamaChatCompletion(
                modelId: llmSettings.Ollama.ModelId,
                endpoint: new Uri(llmSettings.Ollama.Endpoint));

            kernelBuilder.AddOllamaChatCompletion(
                serviceId: "Gatekeeper",
                modelId: llmSettings.Ollama.GatekeeperModelId,
                endpoint: new Uri(llmSettings.Ollama.Endpoint));

            services.AddScoped<IClusterSummaryService, SemanticKernelClusterSummaryService>();
        }
        else if (llmSettings.Provider == "OpenAI")
        {
            kernelBuilder.AddOpenAIChatCompletion(llmSettings.OpenAI.ModelId, llmSettings.OpenAI.ApiKey);
            services.AddScoped<IClusterSummaryService, SemanticKernelClusterSummaryService>();
        }
        else if (llmSettings.Provider == "Bedrock")
        {
            kernelBuilder.AddBedrockChatCompletionService(
                modelId: llmSettings.Bedrock.ModelId);

            kernelBuilder.AddBedrockChatCompletionService(
                serviceId: "Gatekeeper",
                modelId: llmSettings.Bedrock.GatekeeperModelId);

            services.AddScoped<IClusterSummaryService, SemanticKernelClusterSummaryService>();
        }
        else
        {
            services.AddScoped<IClusterSummaryService, PlaceholderClusterSummaryService>();
        }
    }

    private static void AddDuplicateCheckers(this IServiceCollection services, LlmSettings llmSettings)
    {
        if (llmSettings.Provider == "Ollama")
        {
            services.AddScoped<IDuplicateChecker, OllamaDuplicateChecker>();
        }
        else
        {
            services.AddScoped<IDuplicateChecker, PlaceholderDuplicateChecker>();
        }
    }
}
