using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleEmail;
using FastEndpoints;
using FastEndpoints.Swagger;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Winnow.Server.Domain.Services;
using Winnow.Server.Entities;
using Winnow.Server.Features.Dashboard;
using Winnow.Server.Features.Reports.GenerateSummary;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Infrastructure.HealthChecks;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Integrations.Strategies;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Scheduling;
using Winnow.Server.Infrastructure.Security;
using Winnow.Server.Services.Ai;
using Winnow.Server.Services.Ai.Strategies;
using Winnow.Server.Services.Emails;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Extensions;

internal static class ServiceExtensions
{
    private static readonly string[] HealthCheckReadyTags = ["ready"];

    public static IServiceCollection AddWinnowServices(this IServiceCollection services, IConfiguration config)
    {
        // Multi-tenancy
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IExporterFactory, ExporterFactory>();

        // Security
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddScoped<Winnow.Server.Services.Quota.IQuotaService, Winnow.Server.Services.Quota.QuotaService>();

        // Stripe API Key Configuration
        Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"];

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

        // Register embedding providers as Singleton so ONNX models stay in memory
        services.Scan(scan => scan
            .FromAssemblyOf<IEmbeddingProvider>()
            .AddClasses(classes => classes.AssignableTo<IEmbeddingProvider>())
            .As<IEmbeddingProvider>()
            .WithSingletonLifetime()
        );

        // Core HTTP & caching with resilience pipelines
        // Add typed HTTP clients with standard resilience handlers for external services

        // Configure resilience options
        // Using standard resilience handler with default settings:
        // - Retry: Max 3 retries, exponential backoff starting at 2 seconds
        // - Circuit Breaker: Break for 30 seconds after 5 consecutive failures  
        // - Attempt Timeout: 15 seconds per request

        // Register named HTTP client for exporters with resilience handlers
        // Register the tracker singleton before the HttpClient so it can be injected into the handler
        services.AddSingleton<ExternalIntegrationHealthTracker>();
        services.AddTransient<ExternalIntegrationTrackerHandler>();

        services.AddHttpClient("ExternalIntegrations")
            .RemoveAllLoggers() // Prevent health check polling from spamming logs on failure
            .AddHttpMessageHandler<ExternalIntegrationTrackerHandler>()
            .AddStandardResilienceHandler();

        // Register typed HTTP clients for embedding providers with resilience handlers
        services.AddHttpClient<OpenAiEmbeddingProvider>()
            .AddStandardResilienceHandler();

        services.AddHttpClient<LocalEmbeddingProvider>()
            .AddStandardResilienceHandler();

        // Default fallback client
        services.AddHttpClient();
        services.AddMemoryCache();

        // AI Services
        services.AddSingleton<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<IVectorCalculator, VectorCalculator>();
        services.AddHostedService<ClusterRefinementJob>();
        services.AddHostedService<InvitationCleanupJob>();
        services.AddHostedService<CriticalMassSummaryJob>();
        services.AddHostedService<AdminSeeder>();

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
        services.AddScoped<IClusterService, ClusterService>();

        // Email Service
        var emailSettings = new EmailSettings();
        config.GetSection("EmailSettings").Bind(emailSettings);
        services.AddSingleton(emailSettings);

        if (emailSettings.Provider == "AwsSes")
        {
            services.AddDefaultAWSOptions(config.GetAWSOptions());
            services.AddAWSService<IAmazonSimpleEmailService>();
            services.AddScoped<IEmailService, AwsSesEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }

        services.AddDbContext<WinnowDbContext>((sp, options) =>
        {
            var tenantCtx = sp.GetRequiredService<ITenantContext>();
            options.UseNpgsql(tenantCtx.ConnectionString,
                npgsql =>
                {
                    npgsql.UseVector();
                    npgsql.MigrationsAssembly("Winnow.Server");
                });
        });

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole>()
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

            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue("winnow_auth", out var token))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                }
            };
        })
        .AddScheme<Winnow.Server.Infrastructure.Security.ApiKeyAuthenticationOptions, Winnow.Server.Infrastructure.Security.ApiKeyAuthenticationHandler>(Winnow.Server.Infrastructure.Security.ApiKeyAuthenticationOptions.DefaultScheme, null);

        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<WinnowDbContext>("Database", tags: HealthCheckReadyTags)
            .AddCheck<LlmHealthCheck>("LLM", tags: HealthCheckReadyTags)
            .AddCheck<EmailHealthCheck>("Email", tags: HealthCheckReadyTags)
            .AddCheck<TenantIntegrationsHealthCheck>("TenantIntegrations", tags: HealthCheckReadyTags)
            .AddCheck<S3StorageHealthCheck>("S3Storage", tags: HealthCheckReadyTags);

        // Register custom health check services
        services.AddSingleton<LlmHealthCheck>();
        services.AddSingleton<EmailHealthCheck>();
        services.AddSingleton<TenantIntegrationsHealthCheck>();
        services.AddSingleton<S3StorageHealthCheck>();

        // Register caching and publisher for fast UI loading
        services.AddSingleton<CachedHealthReportService>();
        services.AddSingleton<Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheckPublisher, HealthReportPublisher>();

        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.Zero; // Start immediately on boot
            options.Period = TimeSpan.FromSeconds(5); // Poll every 5 seconds
        });

        // FastEndpoints
        services.AddFastEndpoints();
        services.SwaggerDocument(o =>
        {
            o.ShortSchemaNames = true;
            o.DocumentSettings = s =>
            {
                s.AddAuth("Bearer", new()
                {
                    Name = "Authorization",
                    In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                    Type = NSwag.OpenApiSecuritySchemeType.Http,
                    Scheme = "Bearer",
                    Description = "JWT Authorization header using the Bearer scheme."
                });

                s.AddAuth("ApiKey", new()
                {
                    Name = "X-API-Key",
                    In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                    Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
                    Description = "Enter the Bouncer API Key here."
                });

                s.AddAuth("ProjectApiKey", new()
                {
                    Name = "X-Winnow-Key",
                    In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                    Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
                    Description = "Enter the Project API Key here."
                });

                s.OperationProcessors.Add(new Winnow.Server.Infrastructure.Security.Swagger.SwaggerSecurityProcessor());
            };
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireVerifiedEmail", policy =>
                policy.RequireClaim("email_verified", "true"));
        });

        // Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("api", options =>
            {
                options.PermitLimit = 100;
                options.Window = TimeSpan.FromMinutes(1);
                options.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 5;
            });

            options.AddFixedWindowLimiter("webhook", options =>
            {
                options.PermitLimit = 20;
                options.Window = TimeSpan.FromSeconds(1);
                options.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 10;
            });

            options.AddFixedWindowLimiter("strict", options =>
            {
                options.PermitLimit = 10;
                options.Window = TimeSpan.FromMinutes(1);
                options.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 0;
            });
        });

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://app.winnowtriage.com", "http://localhost:5173", "http://localhost:5174")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
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

        // MediatR — in-process domain event dispatcher
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
        services.AddScoped<DomainEventDispatcher>();

        return services;
    }
}