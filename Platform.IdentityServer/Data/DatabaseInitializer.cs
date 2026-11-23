using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.IdentityServer.Models;

namespace Platform.IdentityServer.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            var configDb = serviceProvider.GetRequiredService<ConfigurationDbContext>();
            if (configDb.Database.IsRelational())
            {
                await configDb.Database.MigrateAsync();
            }

            var persistedGrantDb = serviceProvider.GetRequiredService<PersistedGrantDbContext>();
            if (persistedGrantDb.Database.IsRelational())
            {
                await persistedGrantDb.Database.MigrateAsync();
            }

            var appDb = serviceProvider.GetRequiredService<ApplicationDbContext>();
            if (appDb.Database.IsRelational())
            {
                await appDb.Database.MigrateAsync();
            }

            // Seed IdentityResources
            if (!await configDb.IdentityResources.AnyAsync())
            {
                foreach (var resource in Config.IdentityResources)
                {
                    configDb.IdentityResources.Add(resource.ToEntity());
                }
                await configDb.SaveChangesAsync();
                logger.LogInformation("Seeded IdentityResources into IdentityServerDb");
            }

            // Seed ApiScopes
            if (!await configDb.ApiScopes.AnyAsync())
            {
                foreach (var scope in Config.ApiScopes)
                {
                    configDb.ApiScopes.Add(scope.ToEntity());
                }
                await configDb.SaveChangesAsync();
                logger.LogInformation("Seeded ApiScopes into IdentityServerDb");
            }

            // Seed ApiResources
            if (!await configDb.ApiResources.AnyAsync())
            {
                foreach (var resource in Config.ApiResources)
                {
                    configDb.ApiResources.Add(resource.ToEntity());
                }
                await configDb.SaveChangesAsync();
                logger.LogInformation("Seeded ApiResources into IdentityServerDb");
            }

            // Seed Clients
            if (!await configDb.Clients.AnyAsync())
            {
                foreach (var client in Config.Clients)
                {
                    configDb.Clients.Add(client.ToEntity());
                }
                await configDb.SaveChangesAsync();
                logger.LogInformation("Seeded Clients (postman, aggregator_service, swagger) into IdentityServerDb");
            }

            // Seed Users
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = ["Admin", "User"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var alice = await userManager.FindByNameAsync("alice");
            if (alice == null)
            {
                alice = new ApplicationUser
                {
                    UserName = "alice",
                    Email = "alice@platform.local",
                    EmailConfirmed = true,
                    FirstName = "Alice",
                    LastName = "Smith"
                };
                var result = await userManager.CreateAsync(alice, "Alice123!Secure");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(alice, "Admin");
                    logger.LogInformation("Seeded user 'alice' (Admin)");
                }
            }

            var bob = await userManager.FindByNameAsync("bob");
            if (bob == null)
            {
                bob = new ApplicationUser
                {
                    UserName = "bob",
                    Email = "bob@platform.local",
                    EmailConfirmed = true,
                    FirstName = "Bob",
                    LastName = "Jones"
                };
                var result = await userManager.CreateAsync(bob, "Bob123!Secure");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(bob, "User");
                    logger.LogInformation("Seeded user 'bob' (User)");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to initialize or migrate IdentityServer database at startup (DB may not be ready).");
        }
    }
}
