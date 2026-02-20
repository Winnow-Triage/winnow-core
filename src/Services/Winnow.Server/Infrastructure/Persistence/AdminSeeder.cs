using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Winnow.Server.Entities;

namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// Seed the SuperAdmin role and optionally promote a designated email to this role.
/// </summary>
public class AdminSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(IServiceProvider serviceProvider, ILogger<AdminSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure SuperAdmin role exists
        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        {
            await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
            _logger.LogInformation("SuperAdmin role created.");
        }
        else
        {
            _logger.LogDebug("SuperAdmin role already exists.");
        }

        // Optional: Promote a designated email (from environment or config) to SuperAdmin
        // For development, you might hardcode an email, but better to read from config
        // var adminEmail = Environment.GetEnvironmentVariable("WINNOW_ADMIN_EMAIL");
        // For now, we'll skip automatic promotion; manual promotion can be done via a script or UI later.
        // Alternatively, we could add a configuration setting for initial admin email.
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}