using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Predictorator.Data;
using Predictorator.Tests.Helpers;
using Predictorator.Services;

namespace Predictorator.Tests;

public class WebAppDiResolutionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceCollection _services;

    public WebAppDiResolutionTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__TableStorage", "UseDevelopmentStorage=true");
        Environment.SetEnvironmentVariable("ApiSettings__RapidApiKey", "test");
        Environment.SetEnvironmentVariable("Resend__ApiToken", "test");
        Environment.SetEnvironmentVariable("Twilio__FromNumber", "+1000000000");
        Environment.SetEnvironmentVariable("Twilio__AccountSid", "sid");
        Environment.SetEnvironmentVariable("Twilio__AuthToken", "token");

        IServiceCollection? descriptors = null;
        var tempFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IDataStore>();
                services.RemoveAll<IGameWeekRepository>();
                services.RemoveAll<IBackgroundJobService>();
                services.AddSingleton<IDataStore, InMemoryDataStore>();
                services.AddSingleton<IGameWeekRepository, InMemoryGameWeekRepository>();
                services.AddSingleton<IBackgroundJobService>(_ => Substitute.For<IBackgroundJobService>());
                descriptors = services;
            });
        });
        tempFactory.CreateClient();
        _factory = tempFactory;
        _services = descriptors!;
    }

    [Fact]
    public void All_services_can_be_resolved()
    {
        using var scope = _factory.Services.CreateScope();
        var coreAssembly = typeof(IFixtureService).Assembly;
        foreach (var descriptor in _services.Where(d =>
            d.ServiceType.Namespace?.StartsWith("Predictorator") == true &&
            (d.ServiceType.Assembly == coreAssembly || d.ImplementationType?.Assembly == coreAssembly)))
        {
            if (descriptor.ServiceType.IsGenericTypeDefinition) continue;
            var service = scope.ServiceProvider.GetService(descriptor.ServiceType);
            Assert.NotNull(service);
        }
    }
}

