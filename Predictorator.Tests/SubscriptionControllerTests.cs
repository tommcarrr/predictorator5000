using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Predictorator.Data;
using Predictorator.Services;
using Predictorator.Models;

namespace Predictorator.Tests;

public class SubscriptionControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SubscriptionControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("SubDb"));
                services.RemoveAll(typeof(IEmailService));
                var email = Substitute.For<IEmailService>();
                services.AddSingleton(email);
            });
        });
    }

    [Fact]
    public async Task Get_Subscribe_Returns_OK()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/Subscription/Subscribe");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Post_Subscribe_Creates_Subscriber()
    {
        var client = _factory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["email"] = "test@example.com" });
        var response = await client.PostAsync("/Subscription/Subscribe", content);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sub = await db.Subscribers.FirstOrDefaultAsync(s => s.Email == "test@example.com");
        Assert.NotNull(sub);
    }
}
