using CoachingFit.Identity.Core.Contracts;
using CoachingFit.Identity.Core.Entities;
using CoachingFit.Identity.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CoachingFit.Identity.Infrastructure.Data.DataSeed
{
    public class DataInitializer(
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole> _roleManager,
        IConfiguration _configuration) : IDataInitializer
    {
        public async Task InitializeAsync()
        {
            await SeedRolesAsync();
            await SeedAdminAsync();
        }

        private async Task SeedRolesAsync()
        {
            string[] roles = [nameof(UserRole.Admin), nameof(UserRole.Coach), nameof(UserRole.Trainee)];

            foreach (var role in roles)
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));
        }

        private async Task SeedAdminAsync()
        {
            var adminEmail = _configuration["Seeding:AdminEmail"];
            var adminPassword = _configuration["Seeding:AdminPassword"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                return;

            if (await _userManager.FindByEmailAsync(adminEmail) is not null)
                return;

            var admin = new ApplicationUser
            {
                FirstName = "Admin",
                LastName = "CoachingFit",
                Email = adminEmail,
                UserName = "admin_coachingfit",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserRole = UserRole.Admin
            };

            var result = await _userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to seed admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            await _userManager.AddToRoleAsync(admin, nameof(UserRole.Admin));

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(admin);
            await _userManager.ConfirmEmailAsync(admin, token);
        }
    }
}
