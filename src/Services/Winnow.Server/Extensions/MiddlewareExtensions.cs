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
        // Skipped in Testing environment — tests use EnsureCreated
        // to build schema from the current EF model.
        if (!app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

            // Suppress PendingModelChangesWarning
            db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
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
                if (ep.EndpointType.Name == "GetMeEndpoint")
                {
                    ep.Options(b => b.RequireRateLimiting("api"));
                }
                else if (ep.EndpointType.Namespace?.StartsWith("Winnow.Server.Features.Auth") == true)
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