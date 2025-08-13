using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Predictorator.Core.Models;
using Predictorator.Data;
using Predictorator.Core.Data;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;
using Resend;
using NSubstitute;

namespace Predictorator.Tests;

public class AdminPageAccessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminPageAccessTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("TableStorage:ConnectionString", "UseDevelopmentStorage=true");
            builder.UseSetting("AdminUser:Email", "admin@example.com");
            builder.UseSetting("AdminUser:Password", "Admin123!");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDataStore, InMemoryDataStore>();
                services.AddSingleton<IGameWeekService, FakeGameWeekService>();
                services.AddSingleton<IResend>(_ => Substitute.For<IResend>());
                services.AddSingleton<ITwilioSmsSender>(_ => Substitute.For<ITwilioSmsSender>());
                services.AddSingleton<IBackgroundJobService>(_ => Substitute.For<IBackgroundJobService>());
            });
        });
    }

    [Fact]
    public async Task Can_access_admin_page_after_login()
    {
        using var scope = _factory.Services.CreateScope();
        await ApplicationDbInitializer.SeedAdminUserAsync(scope.ServiceProvider);
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginResponse = await client.PostAsJsonAsync("/login", new LoginRequest("admin@example.com", "Admin123!"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var adminResponse = await client.GetAsync("/admin");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }
}

