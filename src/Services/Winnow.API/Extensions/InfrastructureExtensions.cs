using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Persistence.Repositories;
using Winnow.API.Services.Storage;
using Winnow.API.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Projects;
using Winnow.API.Domain.Reports;
using Winnow.API.Domain.Teams;
using Winnow.API.Domain.Core;
using Winnow.API.Services.Caching;
using Winnow.API.Infrastructure.Scheduling;
using Winnow.API.Infrastructure.Integrations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.API.Infrastructure.HealthChecks;

namespace Winnow.API.Extensions;

internal static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        // Multi-tenancy
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IExporterFactory, ExporterFactory>();

        // Caching
        services.AddWinnowCaching(config);

        // Storage (S3/MinIO)
        services.AddWinnowStorage(config);

        // Database
        services.AddDbConfiguration(config);

        // Repositories
        services.AddWinnowRepositories();
        services.AddScoped<DomainEventInterceptor>();

        // Health Checks
        services.AddWinnowHealthChecks();

        // Hosted Services
        services.AddHostedService<InvitationCleanupJob>();
        services.AddHostedService<ApiKeyCleanupJob>();
        services.AddHostedService<DatabaseSweeper>();

        return services;
    }

    private static void AddWinnowCaching(this IServiceCollection services, IConfiguration config)
    {
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
    }

    private static void AddWinnowStorage(this IServiceCollection services, IConfiguration config)
    {
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

    private static void AddWinnowRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IOrganizationRepository, EfOrganizationRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<IReportRepository, EfReportRepository>();
        services.AddScoped<ITeamRepository, EfTeamRepository>();
        services.AddScoped<Winnow.API.Features.Reports.Search.IReportSearchRepository, Winnow.API.Features.Reports.Search.ReportSearchRepository>();
        services.AddScoped<Winnow.API.Features.Clusters.Search.IClusterSearchRepository, Winnow.API.Features.Clusters.Search.ClusterSearchRepository>();
    }

    private static void AddWinnowHealthChecks(this IServiceCollection services)
    {
        services.AddSingleton<Winnow.API.Infrastructure.HealthChecks.CachedHealthReportService>();
        services.AddSingleton<Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheckPublisher, Winnow.API.Infrastructure.HealthChecks.HealthReportPublisher>();
        services.AddSingleton<Winnow.API.Infrastructure.HealthChecks.ExternalIntegrationHealthTracker>();

        services.AddHealthChecks()
            .AddDbContextCheck<WinnowDbContext>()
            .AddCheck<TenantIntegrationsHealthCheck>("Integrations")
            .AddCheck<S3StorageHealthCheck>("Storage")
            .AddCheck<EmailHealthCheck>("Email")
            .AddCheck<LlmHealthCheck>("AI")
            .AddCheck<MemoryHealthCheck>("Memory");

        // Configure the publisher to fire regularly
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.FromSeconds(2);
            options.Period = TimeSpan.FromSeconds(30);
        });
    }

    internal class MemoryHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var used = GC.GetTotalMemory(false);
            var status = HealthStatus.Healthy;
            return Task.FromResult(new HealthCheckResult(
                status,
                description: $"Managed memory used: {used / 1024 / 1024}MB"));
        }
    }
}
