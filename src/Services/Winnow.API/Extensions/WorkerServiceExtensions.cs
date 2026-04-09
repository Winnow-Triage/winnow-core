using Npgsql;
using Amazon;
using Amazon.Comprehend;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleEmail;
using Resend;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Amazon;
using Winnow.API.Infrastructure.Analysis;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Infrastructure.Integrations;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;
using Winnow.API.Services.Ai.Strategies;
using Winnow.API.Services.Caching;
using Winnow.API.Services.Storage;
using Winnow.API.Features.Dashboard.IService;
using Winnow.API.Features.Dashboard.Service;
using Winnow.API.Features.Clusters.GenerateSummary;
using Winnow.API.Domain.Services; // For IVectorCalculator
using Amazon.S3;
using Winnow.API.Services.Emails;
namespace Winnow.API.Extensions;

public static class WorkerServiceExtensions
{
    public static IServiceCollection AddWinnowBaseInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Multi-tenancy context (required for resolving DB connection string)
        services.AddScoped<ITenantContext, TenantContext>();

        // Caching (PoW replay protection and AI mismatch cache)
        var redisConn = config.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConn))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
                options.InstanceName = "Winnow:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
        services.AddSingleton<ICacheService, DistributedCacheService>();

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
                ServiceURL = string.IsNullOrWhiteSpace(s3Settings.Endpoint) ? null : s3Settings.Endpoint,
                RegionEndpoint = string.IsNullOrWhiteSpace(s3Settings.Region) ? null : Amazon.RegionEndpoint.GetBySystemName(s3Settings.Region),
                ForcePathStyle = s3Settings.ForcePathStyle,
                UseHttp = !string.IsNullOrWhiteSpace(s3Settings.Endpoint) && s3Settings.Endpoint.StartsWith("http://")
            };

            if (string.IsNullOrWhiteSpace(s3Settings.AccessKey))
            {
                // Fallback to Default Credential Chain (IAM Role) if no keys are provided
                return new AmazonS3Client(s3Config);
            }

            return new AmazonS3Client(s3Settings.AccessKey, s3Settings.SecretKey, s3Config);
        });
        services.AddSingleton<IStorageService, S3StorageService>();

        // Database
        services.AddScoped<DomainEventInterceptor>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(WinnowDbContext).Assembly);
        });

        // Register NpgsqlDataSource as a Singleton to ensure connection pooling works correctly
        // and to prevent leaks caused by creating new DataSources inside shared scoped delegates.
        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connStr = config.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Postgres connection string missing.");

            // Bridge for AWS-managed passwords or GitHub secrets
            var dbPassword = config["DB_PASSWORD"];
            if (!string.IsNullOrEmpty(dbPassword) && connStr.Contains("{password}"))
            {
                connStr = connStr.Replace("{password}", dbPassword);
            }

            // Bridge for SSL Certificate Download (Optional/Portability)
            config.EnsureRdsSslCertificate();

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
            dataSourceBuilder.UseVector();
            return dataSourceBuilder.Build();
        });

        services.AddDbContext<WinnowDbContext>((sp, options) =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();

            options.UseNpgsql(dataSource,
                npgsql =>
                {
                    npgsql.UseVector();
                    npgsql.MigrationsAssembly("Winnow.API");
                });
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
        });

        // Conditionally inject Message Broker components to maintain local development without AWS credentials
        // This MUST be in the base infrastructure so workers (winnow-summary) receive it.
        if (Environment.GetEnvironmentVariable("MESSAGE_BROKER")?.Equals("AmazonSqs", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddAWSService<Amazon.SQS.IAmazonSQS>();
            services.AddAWSService<Amazon.SimpleNotificationService.IAmazonSimpleNotificationService>();
        }

        // Email Service
        var emailSettings = new Winnow.API.Infrastructure.Configuration.EmailSettings();
        config.GetSection("EmailSettings").Bind(emailSettings);
        services.AddSingleton(emailSettings);

        // Discord Configuration
        services.Configure<DiscordOps>(config.GetSection("DiscordOps"));

        if (emailSettings.Provider == "AwsSes")
        {
            services.AddAWSService<IAmazonSimpleEmailService>();
            services.AddScoped<IEmailService, Winnow.API.Services.Emails.AwsSesEmailService>();
        }
        else if (emailSettings.Provider == "Resend")
        {
            services.AddHttpClient<IResend, ResendClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.resend.com/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", emailSettings.Resend.ApiKey);
            });
            services.AddScoped<IEmailService, ResendEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, Winnow.API.Services.Emails.SmtpEmailService>();
        }

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

        services.AddKernel();

        if (llmSettings.Provider == "Ollama" && !string.IsNullOrEmpty(llmSettings.Ollama?.Endpoint))
        {
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
            services.AddOpenAIChatCompletion(
                modelId: llmSettings.OpenAI.ModelId,
                apiKey: llmSettings.OpenAI.ApiKey);
        }
        else if (llmSettings.Provider == "Bedrock")
        {
            services.AddBedrockChatCompletionService(
                modelId: llmSettings.Bedrock.ModelId);

            services.AddBedrockChatCompletionService(
                serviceId: "Gatekeeper",
                modelId: llmSettings.Bedrock.GatekeeperModelId);
        }

        return services;
    }

    public static IServiceCollection AddWinnowClusteringInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddWinnowKernel(config);

        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);

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
        services.AddWinnowKernel(config);
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);

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
