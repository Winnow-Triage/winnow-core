using System;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.Server.Infrastructure.HealthChecks;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Extensions;

internal static class MiddlewareExtensions
{
    public static async Task<WebApplication> UseWinnowMiddleware(this WebApplication app)
    {
        // Database migration and seeding (startup logic)
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

            db.Database.Migrate();

            // Ensure S3 buckets exist
            try
            {
                var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
                await storage.EnsureBucketsExistAsync();
                Console.WriteLine("S3 buckets verified/created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not ensure S3 buckets exist — MinIO may not be running. {ex.Message}");
            }

            // SQLite multi-tenancy: Apply schema changes to ALL tenant databases.
            // This is only relevant for SQLite mode where each tenant has a separate .db file.
            var dbProvider = app.Configuration["DatabaseProvider"] ?? "Sqlite";
            if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                var dataDir = Path.Combine(app.Environment.ContentRootPath, "Data");
                if (Directory.Exists(dataDir))
                {
                    var dbFiles = Directory.GetFiles(dataDir, "*.db");
                    foreach (var dbFile in dbFiles)
                    {
                        var connectionString = $"Data Source={dbFile}";

                        var optionsBuilder = new DbContextOptionsBuilder<WinnowDbContext>();
                        optionsBuilder.UseSqlite(connectionString);

                        using var tenantDb = new WinnowDbContext(optionsBuilder.Options, null!, app.Configuration);

                        try
                        {
                            tenantDb.Database.Migrate();

                            // Seed a placeholder user first to avoid FK constraint failure
                            tenantDb.Database.ExecuteSqlRaw(@"
                                INSERT OR IGNORE INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, CreatedAt)
                                VALUES ('00000000-0000-0000-0000-000000000001', 'system@winnow.com', 'SYSTEM@WINNOW.COM', 'system@winnow.com', 1, 'AQAAAAIAAYagAAAAEJ...', 'stamp', 'stamp', 0, 0, 0, 0, 'System User', '2024-01-01');
                            ");

                            tenantDb.Database.ExecuteSqlRaw(@"
                                INSERT OR IGNORE INTO Projects (Id, Name, ApiKey, CreatedAt, OwnerId)
                                VALUES ('00000000-0000-0000-0000-000000000001', 'Default Project', 'wm_live_' || substr(hex(randomblob(16)), 1, 20), '2024-01-01', '00000000-0000-0000-0000-000000000001');
                            ");

                            // Ensure vec_reports exists (virtual tables are not in migrations)
                            tenantDb.Database.ExecuteSqlRaw("CREATE VIRTUAL TABLE IF NOT EXISTS vec_reports USING vec0(embedding float[384] distance_metric=cosine);");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to migrate/seed tenant database {dbFile}: {ex.Message}");
                        }
                    }
                }
            }
        }

        // Middleware pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }
        app.UseHttpsRedirection();

        app.UseCors();
        app.UseAuthentication();
        app.UseMiddleware<TenantMiddleware>();
        app.UseAuthorization();
        app.UseStaticFiles();
        app.UseRateLimiter();
        app.UseFastEndpoints(c =>
        {
            c.Endpoints.Configurator = ep =>
            {
                if (ep.EndpointType.Namespace?.StartsWith("Winnow.Server.Features.Auth") == true)
                {
                    ep.Options(b => b.RequireRateLimiting("strict"));
                }
                else if (ep.EndpointType.Name == "IngestReportEndpoint" || ep.EndpointType.Name == "SimulateTrafficEndpoint")
                {
                    ep.Options(b => b.RequireRateLimiting("webhook"));
                }
                else
                {
                    ep.Options(b => b.RequireRateLimiting("api"));
                }
            };
        });
        app.UseSwaggerGen();

        return app;
    }
}