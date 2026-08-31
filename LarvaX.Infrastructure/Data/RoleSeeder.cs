using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LarvaX.Infrastructure.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            string[] roleNames = { "Citizen", "Doctor", "HealthWorker", "LabStaff", "Administrator", "GovernmentAuthority" };
            
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed default Administrator user if not present
            var userManager = serviceProvider.GetRequiredService<UserManager<Core.Entities.ApplicationUser>>();
            var adminEmail = "admin@larvax.gov.bd";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new Core.Entities.ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    PreferredLanguage = "en",
                    ModePreference = "Professional",
                    IsApproved = true,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }
        }
    }
}
