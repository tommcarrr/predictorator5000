using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Predictorator.Data;
using Predictorator.Options;

namespace Predictorator.Tests;

public class AdminUserSeedingTests
{
    private static ServiceProvider BuildServices(Action<IdentityOptions>? identityOpts, string email, string password)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IUserStore<IdentityUser>, InMemoryUserStore>();
        services.AddSingleton<IRoleStore<IdentityRole>, InMemoryRoleStore>();
        services.AddIdentity<IdentityUser, IdentityRole>(identityOpts ?? (_ => { }))
            .AddDefaultTokenProviders();
        services.Configure<AdminUserOptions>(o => { o.Email = email; o.Password = password; });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Throws_when_creation_fails()
    {
        using var provider = BuildServices(opts =>
        {
            opts.Password.RequireDigit = false;
            opts.Password.RequireLowercase = false;
            opts.Password.RequireUppercase = false;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequiredLength = 10;
        }, "admin@example.com", "short");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => AdminUserInitializer.SeedAdminUserAsync(provider));
    }

    [Fact]
    public async Task Creates_admin_user()
    {
        using var provider = BuildServices(null, "admin@example.com", "Admin123!");
        await AdminUserInitializer.SeedAdminUserAsync(provider);
        var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(user);
    }
}

