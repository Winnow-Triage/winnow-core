using FastEndpoints;
using FastEndpoints.Swagger;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.SemanticKernel;
using Winnow.Server.Domain.Services;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Integrations.Strategies;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Scheduling;
using Winnow.Server.Services.Ai;
using Winnow.Server.Services.Ai.Strategies;
using Winnow.Server.Services.Storage;
using Winnow.Server.Features.Dashboard;
using Winnow.Server.Features.Reports.GenerateSummary;

namespace Winnow.Server.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddWinnowServices(this IServiceCollection services, IConfiguration config)
    {
        // Multi-tenancy
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ExporterFactory>();
        
        // Assembly scanning for all strategy implementations
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
        
        // Register embedding providers
        services.Scan(scan => scan
            .FromAssemblyOf<IEmbeddingProvider>()
            .AddClasses(classes => classes.AssignableTo<IEmbeddingProvider>())
            .As<IEmbeddingProvider>()
            .WithScopedLifetime()
        );
        
        // Core HTTP & caching
        services.AddHttpClient();
        services.AddMemoryCache();
        
        // AI Services
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<IVectorCalculator, VectorCalculator>();
        services.AddHostedService<ClusterRefinementJob>();
        
        // Storage (S3/MinIO)
        var s3Settings = new S3Settings();
        config.GetSection("S3Settings").Bind(s3Settings);
        services.AddSingleton(s3Settings);
        services.AddSingleton<Amazon.S3.IAmazonS3>(_ =>
        {
            var s3Config = new Amazon.S3.AmazonS3Config
            {
                ServiceURL = s3Settings.Endpoint,
                ForcePathStyle = true, // Required for MinIO
                UseHttp = s3Settings.Endpoint.StartsWith("http://")
            };
            return new Amazon.S3.AmazonS3Client(s3Settings.AccessKey, s3Settings.SecretKey, s3Config);
        });
        services.AddSingleton<IStorageService, S3StorageService>();
        
        // LLM Configuration
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);
        services.AddSingleton(llmSettings);
        
        // Semantic Kernel setup based on provider
        if (llmSettings.Provider == "Ollama")
        {
            services.AddKernel();
            services.AddOllamaChatCompletion(
                modelId: llmSettings.Ollama.ModelId,
                endpoint: new Uri(llmSettings.Ollama.Endpoint));

            // Secondary model for fast gatekeeping (phi3/gemma)
            services.AddOllamaChatCompletion(
                serviceId: "Gatekeeper",
                modelId: llmSettings.Ollama.GatekeeperModelId,
                endpoint: new Uri(llmSettings.Ollama.Endpoint));

            services.AddScoped<IClusterSummaryService, SemanticKernelClusterSummaryService>();
        }
        else if (llmSettings.Provider == "OpenAI")
        {
            services.AddKernel();
            services.AddOpenAIChatCompletion(
                modelId: llmSettings.OpenAI.ModelId,
                apiKey: llmSettings.OpenAI.ApiKey);
            services.AddScoped<IClusterSummaryService, SemanticKernelClusterSummaryService>();
        }
        else
        {
            services.AddScoped<IClusterSummaryService, PlaceholderSummaryService>();
        }
        
        // Always register the duplicate checker (It handles fail-safe internally)
        services.AddScoped<IDuplicateChecker, OllamaDuplicateChecker>();
        services.AddSingleton<INegativeMatchCache, NegativeMatchCache>();
        
        // Dashboard service
        services.AddScoped<IDashboardService, DashboardService>();
        
        // Database
        services.AddDbContext<WinnowDbContext>(); // Configuration happens in OnConfiguring dynamically
        
        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<WinnowDbContext>()
            .AddDefaultTokenProviders();
        
        // Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtSettings = config.GetSection("JwtSettings");
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "super_secret_key_at_least_32_chars_long_for_safety"))
            };
        });
        
        // FastEndpoints
        services.AddFastEndpoints();
        services.SwaggerDocument();
        services.AddAuthorization();
        
        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
        
        // MassTransit
        services.AddMassTransit(x =>
        {
            x.AddConsumer<Winnow.Server.Features.Reports.Create.ReportCreatedConsumer>();
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });
        
        return services;
    }
}