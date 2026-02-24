using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Winnow.Server.Entities;

namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// Seed the SuperAdmin role, the initial admin user, and the default organization.
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
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var dbContext = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // 1. Ensure SuperAdmin role exists
        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        {
            await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
            _logger.LogInformation("SuperAdmin role created.");
        }

        // 2. Ensure initial SuperAdmin user exists
        var adminEmail = configuration["InitialAdminEmail"] ?? "admin@winnowtriage.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            _logger.LogInformation("Creating initial superadmin user: {Email}", adminEmail);
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, "P@ssword123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                _logger.LogInformation("Initial superadmin user created successfully.");
            }
            else
            {
                _logger.LogError("Failed to create initial superadmin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Ensure they have the role if they already exist
            if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                _logger.LogInformation("Assigned SuperAdmin role to existing user {Email}.", adminEmail);
            }
        }

        // 3. Ensure 'Winnow Admin' organization exists
        var adminOrg = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Name == "Winnow Admin", cancellationToken);

        if (adminOrg == null)
        {
            _logger.LogInformation("Bootstrapping initial 'Winnow Admin' organization.");

            adminOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Winnow Admin",
                SubscriptionTier = "Enterprise",
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Organizations.Add(adminOrg);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Initial 'Winnow Admin' organization created.");
        }

        // 4. Ensure SuperAdmin user is owner of the 'Winnow Admin' organization
        if (adminUser != null)
        {
            var existingMembership = await dbContext.OrganizationMembers
                .IgnoreQueryFilters()
                .AnyAsync(m => m.UserId == adminUser.Id && m.OrganizationId == adminOrg.Id, cancellationToken);

            if (!existingMembership)
            {
                var membership = new OrganizationMember
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUser.Id,
                    OrganizationId = adminOrg.Id,
                    Role = "owner",
                    JoinedAt = DateTime.UtcNow
                };

                dbContext.OrganizationMembers.Add(membership);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Assigned superadmin user {Email} as owner of 'Winnow Admin' organization.", adminEmail);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}