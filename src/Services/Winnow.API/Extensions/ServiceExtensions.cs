using Npgsql;
using Amazon;
using Amazon.Comprehend;
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
using Winnow.API.Domain.Core;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Projects;
using Winnow.API.Domain.Reports;
using Winnow.API.Domain.Services;
using Winnow.API.Domain.Teams;
using Winnow.API.Features.Clusters.GenerateSummary;
using Winnow.API.Features.Dashboard.IService;
using Winnow.API.Features.Dashboard.Service;
using Winnow.API.Infrastructure.Analysis;
using Winnow.API.Infrastructure.Billing;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Infrastructure.HealthChecks;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Integrations;
using Winnow.API.Infrastructure.Integrations.Strategies;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Persistence.Repositories;
using Winnow.API.Infrastructure.Scheduling;
using Winnow.API.Infrastructure.Security;
using Winnow.API.Services.Ai;
using Winnow.API.Services.Ai.Strategies;
using Winnow.API.Services.Emails;
using Winnow.API.Services.Storage;

using Microsoft.Extensions.Hosting;

namespace Winnow.API.Extensions;

internal static class ServiceExtensions
{
    private static readonly string[] HealthCheckReadyTags = ["ready"];

    public static IServiceCollection AddWinnowServices(this IServiceCollection services, IConfiguration config, IHostEnvironment hostEnv)
    {
        // Multi-tenancy
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IExporterFactory, ExporterFactory>();

        // Security
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddScoped<Winnow.API.Services.Quota.IQuotaService, Winnow.API.Services.Quota.QuotaService>();

        // Stripe API Key Configuration
        Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        services.AddSingleton<IStripePlanMapper, StripePlanMapper>();

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

        services.AddHttpClient<LocalPiiRedactionProvider>()
            .AddStandardResilienceHandler();

        // Default fallback client
        services.AddHttpClient();
        services.AddMemoryCache();

        // LLM Configuration
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);
        services.AddSingleton(llmSettings);

        // Email Configuration
        var emailSettings = new EmailSettings();
        config.GetSection("EmailSettings").Bind(emailSettings);
        services.AddSingleton(emailSettings);

        // AWS Configuration (Shared)
        if (llmSettings.ToxicityProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true ||
            llmSettings.PiiRedactionProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true ||
            emailSettings.Provider?.Equals("AwsSes", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddDefaultAWSOptions(config.GetAWSOptions());
        }

        if (llmSettings.ToxicityProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true ||
            llmSettings.PiiRedactionProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddAWSService<IAmazonComprehend>();
        }

        // Toxicity Detection
        services.AddSingleton<IToxicityDetectionProvider, LocalToxicityDetectionProvider>();
        if (llmSettings.ToxicityProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddSingleton<IToxicityDetectionProvider, AwsComprehendToxicityDetectionProvider>();
        }
        services.AddSingleton<IToxicityDetectionService, ToxicityDetectionService>();

        // PII Redaction
        services.AddSingleton<LocalPiiRedactionProvider>();
        services.AddSingleton<IPiiRedactionProvider>(sp => sp.GetRequiredService<LocalPiiRedactionProvider>());
        if (llmSettings.PiiRedactionProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddSingleton<IPiiRedactionProvider, AwsComprehendPiiRedactionProvider>();
        }
        services.AddSingleton<IPiiRedactionService, PiiRedactionService>();

        services.AddHostedService<AdminSeeder>();
        services.AddHostedService<InvitationCleanupJob>();
        services.AddHostedService<DatabaseSweeper>();

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

        // AI Services
        services.AddScoped<ClusterSummaryOrchestrator>();
        services.AddSingleton<IEmbeddingService, EmbeddingService>();


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

        services.AddSingleton<IVectorCalculator, VectorCalculator>();

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

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IOrganizationRepository, EfOrganizationRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<IReportRepository, EfReportRepository>();
        services.AddScoped<ITeamRepository, EfTeamRepository>();

        // Register new ReportSearchRepository for raw SQL Hybrid semantic queries
        services.AddScoped<Winnow.API.Features.Reports.Search.IReportSearchRepository, Winnow.API.Features.Reports.Search.ReportSearchRepository>();
        services.AddScoped<Winnow.API.Features.Clusters.Search.IClusterSearchRepository, Winnow.API.Features.Clusters.Search.ClusterSearchRepository>();

        // Always register the duplicate checker (It handles fail-safe internally)
        services.AddScoped<IDuplicateChecker, OllamaDuplicateChecker>();
        services.AddSingleton<INegativeMatchCache, NegativeMatchCache>();

        // Dashboard service
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IClusterService, ClusterService>();

        if (emailSettings.Provider == "AwsSes")
        {
            services.AddAWSService<IAmazonSimpleEmailService>();
            services.AddScoped<IEmailService, AwsSesEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }

        // Register NpgsqlDataSource as a Singleton to ensure connection pooling works correctly
        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connStr = config.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Postgres connection string missing.");

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
        .AddScheme<Winnow.API.Infrastructure.Security.ApiKeyAuthenticationOptions, Winnow.API.Infrastructure.Security.ApiKeyAuthenticationHandler>(Winnow.API.Infrastructure.Security.ApiKeyAuthenticationOptions.DefaultScheme, null);

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

                s.OperationProcessors.Add(new Winnow.API.Infrastructure.Security.Swagger.SwaggerSecurityProcessor());
                s.OperationProcessors.Add(new Winnow.API.Infrastructure.Security.Swagger.MediatRAuthOperationProcessor());
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
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromHours(2));
            });
        });

        // MassTransit
        services.AddWinnowMassTransit(config, hostEnv, enableOutbox: true);

        // MediatR — in-process domain event dispatcher
        // DomainEventInterceptor automatically dispatches events from all IAggregateRoot
        // implementations after each SaveChangesAsync — no manual wiring needed.
        services.AddHttpContextAccessor();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Program>();
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(Winnow.API.Infrastructure.Security.Authorization.AuthorizationBehavior<,>));
        });
        services.AddScoped<DomainEventInterceptor>();

        return services;
    }
}