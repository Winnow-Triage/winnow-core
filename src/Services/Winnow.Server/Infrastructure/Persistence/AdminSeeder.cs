using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Common;
using Winnow.Server.Domain.Organizations.ValueObjects;
using Winnow.Server.Infrastructure.Identity;

namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// Seed the SuperAdmin role, the initial admin user, and the default organization.
/// </summary>
public class AdminSeeder(IServiceProvider serviceProvider, ILogger<AdminSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var dbContext = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // 1. Ensure SuperAdmin role exists
        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        {
            await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
            logger.LogInformation("SuperAdmin role created.");
        }

        // 2. Ensure initial SuperAdmin user exists
        var adminEmail = configuration["InitialAdminEmail"] ?? "admin@winnowtriage.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            logger.LogInformation("Creating initial superadmin user: {Email}", adminEmail);
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
                logger.LogInformation("Initial superadmin user created successfully.");
            }
            else
            {
                logger.LogError("Failed to create initial superadmin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Ensure they have the role if they already exist
            if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                logger.LogInformation("Assigned SuperAdmin role to existing user {Email}.", adminEmail);
            }
        }

        // Ensure System Roles exist
        var systemRoles = new[] { "Owner", "Admin", "Member" };
        var ownerRoleId = Guid.Empty;
        var adminRoleId = Guid.Empty;
        var memberRoleId = Guid.Empty;

        foreach (var roleName in systemRoles)
        {
            var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.OrganizationId == null, cancellationToken);
            if (role == null)
            {
                role = new Domain.Security.Role(roleName, null);
                dbContext.Roles.Add(role);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("System role '{RoleName}' created.", roleName);
            }
            if (roleName == "Owner") ownerRoleId = role.Id;
            else if (roleName == "Admin") adminRoleId = role.Id;
            else if (roleName == "Member") memberRoleId = role.Id;
        }

        // 3. Ensure 'Winnow Admin' organization exists
        var adminOrg = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Name == "Winnow Admin", cancellationToken);

        if (adminOrg == null)
        {
            logger.LogInformation("Bootstrapping initial 'Winnow Admin' organization.");

            adminOrg = new Domain.Organizations.Organization(
                "Winnow Admin",
                new Email("admin@winnowtriage.com"),
                SubscriptionPlan.Enterprise
            );

            dbContext.Organizations.Add(adminOrg);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Initial 'Winnow Admin' organization created.");
        }

        // 4. Ensure 'Winnow Admin' has a default project
        var adminProject = await dbContext.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.OrganizationId == adminOrg.Id, cancellationToken);

        if (adminProject == null)
        {
            logger.LogInformation("Creating default project for 'Winnow Admin' organization.");
            var apiKeyService = scope.ServiceProvider.GetRequiredService<Security.IApiKeyService>();
            var projectId = Guid.NewGuid();
            var plaintextKey = apiKeyService.GeneratePlaintextKey(projectId);

            adminProject = new Domain.Projects.Project
            (
                adminOrg.Id,
                "Default Project",
                adminUser?.Id ?? "",
                apiKeyService.HashKey(plaintextKey)
            );

            dbContext.Projects.Add(adminProject);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Default project created for 'Winnow Admin'. API Key: {Key}", plaintextKey);
        }

        // Ensure SuperAdmin user is owner of the 'Winnow Admin' organization
        if (adminUser != null)
        {
            // Force the dbContext to acknowledge the user exists 
            // even though a different manager created them.
            if (dbContext.Entry(adminUser).State == EntityState.Detached)
            {
                dbContext.Users.Attach(adminUser);
            }

            var finalOrgId = adminOrg.Id;
            var finalUserId = adminUser.Id;

            // Use the dbContext to check for existence so it stays in the same transaction
            var existingMembership = await dbContext.OrganizationMembers
                .IgnoreQueryFilters()
                .AnyAsync(m => m.UserId == finalUserId && m.OrganizationId == finalOrgId, cancellationToken);

            if (!existingMembership)
            {
                logger.LogInformation("Creating membership link for {Email} and {OrgName}", adminEmail, adminOrg.Name);

                var membership = new Winnow.Server.Domain.Organizations.OrganizationMember(
                    finalOrgId,
                    finalUserId,
                    ownerRoleId);

                dbContext.OrganizationMembers.Add(membership);

                // Final save should now succeed because the User and Org are both 'tracked'
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Successfully assigned owner role.");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}