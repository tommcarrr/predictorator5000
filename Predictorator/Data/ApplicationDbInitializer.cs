using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Predictorator.Core.Options;

namespace Predictorator.Data;

public static class ApplicationDbInitializer
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        const string adminRole = "Admin";
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<AdminUserOptions>>().Value;
        var adminEmail = options.Email;
        var adminPassword = options.Password;

        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, adminPassword);
            if (!result.Succeeded)
            {
                user = await userManager.FindByEmailAsync(adminEmail);
                if (user == null)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create admin user: {errors}");
                }
            }
        }

        if (!await userManager.IsInRoleAsync(user, adminRole))
        {
            await userManager.AddToRoleAsync(user, adminRole);
        }
    }
}
