using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Predictorator.Options;
using Predictorator.Data;

namespace Predictorator.Tests;

public class AdminUserSeedingTests
{
    private static ServiceProvider BuildServices(Action<IdentityOptions>? identityOpts, string email, string password)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentity<IdentityUser, IdentityRole>(identityOpts ?? (_ => { }));
        services.AddSingleton<IUserStore<IdentityUser>, InMemoryUserStore>();
        services.AddSingleton<IRoleStore<IdentityRole>, InMemoryRoleStore>();
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
            () => ApplicationDbInitializer.SeedAdminUserAsync(provider));
    }

    [Fact]
    public async Task Can_sign_in_with_seeded_admin_user()
    {
        const string email = "admin@example.com";
        const string password = "Admin123!";
        using var provider = BuildServices(null, email, password);
        await ApplicationDbInitializer.SeedAdminUserAsync(provider);

        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(await userManager.CheckPasswordAsync(user!, password));
    }
}

