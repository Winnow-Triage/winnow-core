using Npgsql;
using Winnow.API.Infrastructure.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Winnow.API.Services.Emails;
using Amazon;
using Amazon.Comprehend;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleEmail;
using Resend;
using FastEndpoints;
using FastEndpoints.Swagger;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Amazon;
using System.Net.Http.Headers;
using Winnow.API.Domain.Core;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Projects;
using Winnow.API.Domain.Reports;
using Winnow.API.Domain.Services;
using Winnow.API.Domain.Teams;
using Winnow.API.Features.Clusters.GenerateSummary;
using Winnow.API.Features.Dashboard.IService;
using Winnow.API.Features.Dashboard.Service;
using Winnow.API.Services.Discord;
using Winnow.API.Infrastructure.Analysis;
using Winnow.API.Infrastructure.Billing;
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
    private const string AmazonComprehend = "AmazonComprehend";

    public static IServiceCollection AddWinnowServices(this IServiceCollection services, IConfiguration config, IHostEnvironment hostEnv)
    {
        services.AddInfrastructureServices(config);
        services.AddAiAndLlmServices(config);
        services.AddSecurityAndIdentity(config);
        services.AddEmailAndNotifications(config);
        services.AddMiddlewareAndPolicies(config);

        return services;
    }

    private static void AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        // Multi-tenancy
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IExporterFactory, ExporterFactory>();

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
        services.AddMemoryCache();

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
                return new Amazon.S3.AmazonS3Client(s3Config);
            }

            return new Amazon.S3.AmazonS3Client(s3Settings.AccessKey, s3Settings.SecretKey, s3Config);
        });
        services.AddSingleton<IStorageService, S3StorageService>();

        // Database
        services.AddDbConfiguration(config);

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IOrganizationRepository, EfOrganizationRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<IReportRepository, EfReportRepository>();
        services.AddScoped<ITeamRepository, EfTeamRepository>();
        services.AddScoped<Winnow.API.Features.Reports.Search.IReportSearchRepository, Winnow.API.Features.Reports.Search.ReportSearchRepository>();
        services.AddScoped<Winnow.API.Features.Clusters.Search.IClusterSearchRepository, Winnow.API.Features.Clusters.Search.ClusterSearchRepository>();

        // Health Checks
        services.AddWinnowHealthChecks();

        // Hosted Services
        services.AddHostedService<AdminSeeder>();
        services.AddHostedService<InvitationCleanupJob>();
        services.AddHostedService<ApiKeyCleanupJob>();
        services.AddHostedService<DatabaseSweeper>();
    }

    private static void AddDbConfiguration(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var connStr = config.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Postgres connection string missing.");

            var dbPassword = config["DB_PASSWORD"];
            if (!string.IsNullOrEmpty(dbPassword) && connStr.Contains("{password}"))
            {
                connStr = connStr.Replace("{password}", dbPassword);
            }

            config.EnsureRdsSslCertificate();

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
            dataSourceBuilder.UseVector();
            return dataSourceBuilder.Build();
        });

        services.AddDbContext<WinnowDbContext>((sp, options) =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource, npgsql =>
            {
                npgsql.UseVector();
                npgsql.MigrationsAssembly("Winnow.API");
            });
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
        });
    }

    private static void AddAiAndLlmServices(this IServiceCollection services, IConfiguration config)
    {
        var llmSettings = new LlmSettings();
        config.GetSection("LlmSettings").Bind(llmSettings);
        services.AddSingleton(llmSettings);

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
        if (llmSettings.Provider == "Ollama")
        {
            services.AddScoped<IDuplicateChecker, OllamaDuplicateChecker>();
        }
        else
        {
            services.AddScoped<IDuplicateChecker, PlaceholderDuplicateChecker>();
        }
        services.AddSingleton<INegativeMatchCache, NegativeMatchCache>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IClusterService, ClusterService>();
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

    private static void AddWinnowKernel(this IServiceCollection services, LlmSettings llmSettings)
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

    private static void AddSecurityAndIdentity(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddScoped<Winnow.API.Services.Quota.IQuotaService, Winnow.API.Services.Quota.QuotaService>();

        // Proof-of-Work
        services.Configure<PoWSettings>(config.GetSection("PoWSettings"));
        services.AddSingleton<IPoWValidator, PoWValidator>();

        // Stripe
        Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        services.AddSingleton<IStripePlanMapper, StripePlanMapper>();

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<WinnowDbContext>()
        .AddDefaultTokenProviders();

        // Authentication
        services.AddWinnowAuthentication(config);
    }

    private static void AddWinnowAuthentication(this IServiceCollection services, IConfiguration config)
    {
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
                    System.Text.Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey configuration is missing")))
            };

            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue("winnow_auth", out var token))
                        context.Token = token;
                    return Task.CompletedTask;
                }
            };
        })
        .AddScheme<Winnow.API.Infrastructure.Security.ApiKeyAuthenticationOptions, Winnow.API.Infrastructure.Security.ApiKeyAuthenticationHandler>(Winnow.API.Infrastructure.Security.ApiKeyAuthenticationOptions.DefaultScheme, null);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireVerifiedEmail", policy => policy.RequireClaim("email_verified", "true"));
        });
    }

    private static void AddEmailAndNotifications(this IServiceCollection services, IConfiguration config)
    {
        var emailSettings = new Winnow.API.Infrastructure.Configuration.EmailSettings();
        config.GetSection("EmailSettings").Bind(emailSettings);
        services.AddSingleton(emailSettings);

        // Discord
        services.Configure<DiscordOps>(config.GetSection("DiscordOps"));
        services.AddScoped<IInternalOpsNotifier, InternalOpsNotifier>();
        services.AddScoped<IClientNotificationService, ClientNotificationService>();

        if (emailSettings.Provider == "AwsSes")
        {
            services.AddAWSService<IAmazonSimpleEmailService>();
            services.AddScoped<IEmailService, AwsSesEmailService>();
        }
        else if (emailSettings.Provider == "Resend")
        {
            if (string.IsNullOrWhiteSpace(emailSettings.Resend.ApiKey))
                Console.WriteLine("[WARNING] Resend API Key is missing or empty. Emails will likely fail.");

            services.Configure<ResendClientOptions>(options => options.ApiToken = emailSettings.Resend.ApiKey);
            services.AddHttpClient<IResend, ResendClient>(client =>
                client.BaseAddress = new Uri(emailSettings.Resend.BaseUrl));
            services.AddScoped<IEmailService, ResendEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
    }

    private static void AddMiddlewareAndPolicies(this IServiceCollection services, IConfiguration config)
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
        services.AddSingleton<ExternalIntegrationHealthTracker>();
        services.AddTransient<ExternalIntegrationTrackerHandler>();
        services.AddHttpClient("ExternalIntegrations")
            .RemoveAllLoggers()
            .AddHttpMessageHandler<ExternalIntegrationTrackerHandler>()
            .AddStandardResilienceHandler();

        services.AddHttpClient<OpenAiEmbeddingProvider>().AddStandardResilienceHandler();
        services.AddHttpClient<LocalEmbeddingProvider>().AddStandardResilienceHandler();
        services.AddHttpClient<LocalPiiRedactionProvider>().AddStandardResilienceHandler();
        services.AddHttpClient();

        // AWS Shared
        services.AddDefaultAWSOptions(config.GetAWSOptions());

        // FastEndpoints & Swagger
        services.AddWinnowOpenApi();

        // Middleware Config
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = null;
        });

        // Rate Limiting
        services.AddWinnowRateLimiting();

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://app.winnowtriage.com", "https://winnowtriage.com", "https://www.winnowtriage.com", "http://localhost:5173", "http://localhost:5174")
                      .AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetPreflightMaxAge(TimeSpan.FromHours(2));
            });
        });

        // MediatR
        services.AddHttpContextAccessor();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Program>();
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(Winnow.API.Infrastructure.Security.Authorization.AuthorizationBehavior<,>));
        });
        services.AddScoped<DomainEventInterceptor>();
    }

    private static void AddWinnowHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<WinnowDbContext>("Database", tags: HealthCheckReadyTags)
            .AddCheck<LlmHealthCheck>("LLM", tags: HealthCheckReadyTags)
            .AddCheck<EmailHealthCheck>("Email", tags: HealthCheckReadyTags)
            .AddCheck<TenantIntegrationsHealthCheck>("TenantIntegrations", tags: HealthCheckReadyTags)
            .AddCheck<S3StorageHealthCheck>("S3Storage", tags: HealthCheckReadyTags);

        services.AddSingleton<LlmHealthCheck>();
        services.AddSingleton<EmailHealthCheck>();
        services.AddSingleton<TenantIntegrationsHealthCheck>();
        services.AddSingleton<S3StorageHealthCheck>();

        services.AddSingleton<CachedHealthReportService>();
        services.AddSingleton<Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheckPublisher, HealthReportPublisher>();

        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.Zero;
            options.Period = TimeSpan.FromSeconds(5);
        });
    }

    private static void AddWinnowOpenApi(this IServiceCollection services)
    {
        services.AddFastEndpoints();
        services.SwaggerDocument(o =>
        {
            o.ShortSchemaNames = true;
            o.DocumentSettings = s =>
            {
                s.AddAuth("Bearer", new() { Name = "Authorization", In = NSwag.OpenApiSecurityApiKeyLocation.Header, Type = NSwag.OpenApiSecuritySchemeType.Http, Scheme = "Bearer", Description = "JWT Authorization header using the Bearer scheme." });
                s.AddAuth("ApiKey", new() { Name = "X-API-Key", In = NSwag.OpenApiSecurityApiKeyLocation.Header, Type = NSwag.OpenApiSecuritySchemeType.ApiKey, Description = "Enter the Bouncer API Key here." });
                s.AddAuth("ProjectApiKey", new() { Name = "X-Winnow-Key", In = NSwag.OpenApiSecurityApiKeyLocation.Header, Type = NSwag.OpenApiSecuritySchemeType.ApiKey, Description = "Enter the Project API Key here." });
                s.OperationProcessors.Add(new Winnow.API.Infrastructure.Security.Swagger.SwaggerSecurityProcessor());
                s.OperationProcessors.Add(new Winnow.API.Infrastructure.Security.Swagger.MediatRAuthOperationProcessor());
            };
        });
    }

    private static void AddWinnowRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions { PermitLimit = 5000, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })),
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var ip = GetIpAddress(context);
                    return RateLimitPartition.GetConcurrencyLimiter(ip, _ => new ConcurrencyLimiterOptions { PermitLimit = 5, QueueLimit = 0 });
                })
            );

            options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(GetIpAddress(context), _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1), QueueLimit = 5 }));
            options.AddPolicy("webhook", context => RateLimitPartition.GetTokenBucketLimiter(GetIpAddress(context), _ => new TokenBucketRateLimiterOptions { TokenLimit = 5, QueueLimit = 0, ReplenishmentPeriod = TimeSpan.FromMinutes(1), TokensPerPeriod = 5, AutoReplenishment = true }));
            options.AddPolicy("strict", context => RateLimitPartition.GetFixedWindowLimiter(GetIpAddress(context), _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("email_dispatch", context => RateLimitPartition.GetFixedWindowLimiter(GetIpAddress(context), _ => new FixedWindowRateLimiterOptions { PermitLimit = 1, Window = TimeSpan.FromMinutes(2), QueueLimit = 0 }));
        });
    }

    private static string GetIpAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "unknown";
}