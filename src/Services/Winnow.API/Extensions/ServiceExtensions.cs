using Npgsql;
using Amazon;
using Amazon.Comprehend;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleEmail;
using FastEndpoints;
using FastEndpoints.Swagger;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Amazon;
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
using Winnow.API.Infrastructure.Security.PoW;
using Winnow.API.Services.Caching;

using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;

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

        // Proof-of-Work
        services.Configure<PoWSettings>(config.GetSection("PoWSettings"));
        services.AddSingleton<IPoWValidator, PoWValidator>();

        // Caching
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
        services.AddDefaultAWSOptions(config.GetAWSOptions());

        if (llmSettings.ToxicityProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true ||
            llmSettings.PiiRedactionProvider?.Equals("AmazonComprehend", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddAWSService<Amazon.Comprehend.IAmazonComprehend>();
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
                ServiceURL = string.IsNullOrWhiteSpace(s3Settings.Endpoint) ? null : s3Settings.Endpoint,
                RegionEndpoint = string.IsNullOrWhiteSpace(s3Settings.Region) ? null : Amazon.RegionEndpoint.GetBySystemName(s3Settings.Region),
                ForcePathStyle = s3Settings.ForcePathStyle,
                UseHttp = !string.IsNullOrWhiteSpace(s3Settings.Endpoint) && s3Settings.Endpoint.StartsWith("http://")
            };


            if (string.IsNullOrWhiteSpace(s3Settings.AccessKey))
            {
                // Fallback to Default Credential Chain (IAM Role) if no keys are provided
                return new Amazon.S3.AmazonS3Client(s3Config);
            }

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
        var kernelBuilder = services.AddKernel();

        if (llmSettings.Provider == "Ollama")
        {
            kernelBuilder.AddOllamaChatCompletion(
                modelId: llmSettings.Ollama.ModelId,
                endpoint: new Uri(llmSettings.Ollama.Endpoint));

            // Secondary model for fast gatekeeping (phi3/gemma)
            kernelBuilder.AddOllamaChatCompletion(
                serviceId: "Gatekeeper",
                modelId: llmSettings.Ollama.GatekeeperModelId,
                endpoint: new Uri(llmSettings.Ollama.Endpoint));

            services.AddScoped<IClusterSummaryService, SemanticKernelClusterSummaryService>();
        }
        else if (llmSettings.Provider == "OpenAI")
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: llmSettings.OpenAI.ModelId,
                apiKey: llmSettings.OpenAI.ApiKey);
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

        // Register the duplicate checker based on provider
        if (llmSettings.Provider == "Ollama")
        {
            services.AddScoped<IDuplicateChecker, OllamaDuplicateChecker>();
        }
        else
        {
            services.AddScoped<IDuplicateChecker, PlaceholderDuplicateChecker>();
        }
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

            // Bridge for AWS-managed passwords or GitHub secrets
            var dbPassword = config["DB_PASSWORD"];
            if (!string.IsNullOrEmpty(dbPassword) && connStr.Contains("{password}"))
            {
                connStr = connStr.Replace("{password}", dbPassword);
            }

            // Bridge for SSL Certificate Download (Optional/Portability)
            var certUrl = config["DB_SSL_CERT_URL"];
            if (!string.IsNullOrEmpty(certUrl) && connStr.Contains("Root Certificate="))
            {
                var match = System.Text.RegularExpressions.Regex.Match(connStr, @"Root Certificate=([^;]+)");
                if (match.Success)
                {
                    var certPath = match.Groups[1].Value.Trim();
                    if (!File.Exists(certPath))
                    {
                        try
                        {
                            using var client = new HttpClient();
                            var certData = client.GetByteArrayAsync(certUrl).GetAwaiter().GetResult();
                            var directory = Path.GetDirectoryName(certPath);
                            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                                Directory.CreateDirectory(directory);
                            File.WriteAllBytes(certPath, certData);
                        }
                        catch { /* Fallback to standard connection if download fails */ }
                    }
                }
            }

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

        // Configure Forwarded Headers for Cloudflare/AWS proxy support
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // In a cloud environment like App Runner/Cloudflare, we trust the incoming proxy
            // clearing KnownProxies and KnownNetworks allows any X-Forwarded-For to be processed
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = null; // Trust all hops
        });

        // Rate Limiting — Layered Defense Depth (Global Safety + Per-IP Bot Protection)
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 1. GLOBAL SAFETY VALVE (Fixed Window) & PER-IP CONCURRENCY
            // Using CreateChained because .NET rate limiting doesn't support multiple endpoint policies natively
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5000,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    })),
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString()
                             ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                             ?? "unknown";
                    return RateLimitPartition.GetConcurrencyLimiter(ip, _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = 5,
                        QueueLimit = 0
                    });
                })
            );

            // 2. PER-IP POLICIES (Protection against targeted abuse)

            // Standard API calls (100 req/min/IP)
            options.AddPolicy("api", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString()
                         ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                         ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 5
                });
            });

            options.AddPolicy("webhook", context =>
            {
                // Try to get IP from RemoteIpAddress (updated by ForwardedHeaders) with a manual fallback for proxies
                var ip = context.Connection.RemoteIpAddress?.ToString()
                         ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                         ?? "unknown";

                return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 5,
                    QueueLimit = 0,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    TokensPerPeriod = 5,
                    AutoReplenishment = true
                });
            });

            // Auth/Sensitive Ops (10 req/min/IP)
            options.AddPolicy("strict", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString()
                         ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                         ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
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