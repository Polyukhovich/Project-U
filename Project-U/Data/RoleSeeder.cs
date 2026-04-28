using Microsoft.AspNetCore.Identity;
using ProjectU.Core.Models;

namespace ProjectU.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Створення ролей якщо їх немає
            string[] roles = { "Admin", "Teacher", "Student" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Створення адміністратора якщо його немає
            string adminEmail = "admin@projectu.com";
            string adminPassword = "Admin123!";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Адмін",
                    LastName = "Системи",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);

                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}