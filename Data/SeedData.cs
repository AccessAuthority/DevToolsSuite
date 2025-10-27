using DevToolsSuite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DevToolsSuite.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            // Create roles
            string[] roleNames = { "Admin", "User", "Pro", "Team" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create admin user
            var adminUser = await userManager.FindByEmailAsync("admin@devtoolssuite.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@devtoolssuite.com",
                    Email = "admin@devtoolssuite.com",
                    DisplayName = "Administrator",
                    SubscriptionPlan = "Pro",
                    CreatedAt = DateTime.UtcNow
                };

                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddToRoleAsync(adminUser, "Pro");
                }
            }

            // Create sample user
            var sampleUser = await userManager.FindByEmailAsync("user@example.com");
            if (sampleUser == null)
            {
                sampleUser = new ApplicationUser
                {
                    UserName = "user@example.com",
                    Email = "user@example.com",
                    DisplayName = "John Doe",
                    SubscriptionPlan = "Free",
                    CreatedAt = DateTime.UtcNow
                };

                var createUser = await userManager.CreateAsync(sampleUser, "User123!");
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(sampleUser, "User");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}