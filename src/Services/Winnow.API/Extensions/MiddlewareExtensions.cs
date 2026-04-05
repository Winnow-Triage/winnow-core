using System;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Winnow.API.Infrastructure.HealthChecks;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Storage;

namespace Winnow.API.Extensions;

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
        app.UseForwardedHeaders();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }
        app.UseHttpsRedirection();

        app.UseRouting(); // REQUIRED for RateLimiting to see endpoint metadata
        app.UseCors();
        app.UseAuthentication();
        app.UseMiddleware<TenantMiddleware>();
        app.UseAuthorization();
        app.UseStaticFiles();

        app.UseRateLimiter(); // Must come after UseRouting and before UseFastEndpoints

        app.UseFastEndpoints(c =>
        {
            c.Serializer.Options.MaxDepth = 16;
            c.Endpoints.Configurator = ep =>
            {
                // Note: Specific rate limits (like 'webhook' for ingestion) are now 
                // applied directly via [EnableRateLimiting] attributes on endpoint classes.
                // This configurator adds the default API & Concurrency limits to all endpoints
                // that haven't been explicitly configured otherwise.
                ep.Options(b => b.RequireRateLimiting("api"));
            };
        });
        app.UseSwaggerGen();

        return app;
    }
}