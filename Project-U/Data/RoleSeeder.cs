using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ProjectU.Core.Models;

namespace ProjectU.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            // Створення ролей якщо їх немає
            string[] roles = { "Admin", "Teacher", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Отримуємо дані адміна з конфігурації
            var adminEmail = configuration["AdminUser:Email"];
            var adminPassword = configuration["AdminUser:Password"];
            var adminFirstName = configuration["AdminUser:FirstName"] ?? "Адмін";
            var adminLastName = configuration["AdminUser:LastName"] ?? "Системи";
            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                Console.WriteLine("WARNING: AdminUser not configured. Set environment variables: AdminUser__Email, AdminUser__Password");
                return;
            }
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = adminFirstName,
                    LastName = adminLastName,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}