using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamTaskManager.Domain.Constants;
using TeamTaskManager.Domain.Entities;

namespace TeamTaskManager.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAdminAsync(this IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("DevelopmentSeed:Enabled"))
        {
            return;
        }

        var email = configuration["DevelopmentSeed:AdminEmail"];
        var password = configuration["DevelopmentSeed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(ApplicationRoles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.Admin));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Sistem",
                LastName = "Yöneticisi",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Geliştirme yöneticisi oluşturulamadı: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Admin);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Yönetici rolü atanamadı: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }
    }
}
