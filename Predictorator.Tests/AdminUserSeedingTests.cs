using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<IdentityUser, IdentityRole>(identityOpts ?? (_ => { }))
            .AddEntityFrameworkStores<ApplicationDbContext>();
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
    public async Task Does_not_throw_when_database_unavailable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=localhost;Database=invalid;" +
                                  "User Id=sa;Password=bad;Connect Timeout=1"));
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.Configure<AdminUserOptions>(o =>
        {
            o.Email = "admin@example.com";
            o.Password = "Admin123!";
        });
        await using var provider = services.BuildServiceProvider();

        await ApplicationDbInitializer.SeedAdminUserAsync(provider);
    }
}

