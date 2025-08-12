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
        services.AddIdentity<IdentityUser, IdentityRole>(identityOpts ?? (_ => { }))
            .AddUserStore<InMemoryUserStore>()
            .AddRoleStore<InMemoryRoleStore>();
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

}

