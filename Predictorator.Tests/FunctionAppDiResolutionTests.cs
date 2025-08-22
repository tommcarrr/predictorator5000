using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Predictorator.Core.Data;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;
using Predictorator.Core.Models;
using Predictorator.Functions;

namespace Predictorator.Tests;

public class FunctionAppDiResolutionTests
{
    private readonly IServiceCollection _services;
    private readonly ServiceProvider _provider;

    public FunctionAppDiResolutionTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:RapidApiKey"] = "test",
                ["Resend:ApiToken"] = "test",
                ["Twilio:FromNumber"] = "+1000000000",
                ["Twilio:AccountSid"] = "sid",
                ["Twilio:AuthToken"] = "token",
                ["GameWeekCache:CacheDurationHours"] = "1",
                ["ConnectionStrings:TableStorage"] = "UseDevelopmentStorage=true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        var env = Substitute.For<IHostEnvironment>();
        env.ContentRootPath.Returns(Directory.GetCurrentDirectory());
        services.AddSingleton<IHostEnvironment>(env);

        services.AddPredictoratorFunctionServices(configuration);

        services.RemoveAll<IEmailSubscriberRepository>();
        services.RemoveAll<ISmsSubscriberRepository>();
        services.RemoveAll<ISentNotificationRepository>();
        services.RemoveAll<TableDataStore>();
        services.RemoveAll<IGameWeekRepository>();
        services.RemoveAll<IBackgroundJobService>();
        services.RemoveAll<IBackgroundJobErrorService>();
        services.RemoveAll<IAnnouncementRepository>();

        var store = new InMemoryDataStore();
        services.AddSingleton<IEmailSubscriberRepository>(store);
        services.AddSingleton<ISmsSubscriberRepository>(store);
        services.AddSingleton<ISentNotificationRepository>(store);
        services.AddSingleton<IGameWeekRepository, InMemoryGameWeekRepository>();
        services.AddSingleton<IBackgroundJobService>(_ => Substitute.For<IBackgroundJobService>());
        services.AddSingleton<IBackgroundJobErrorService>(_ => Substitute.For<IBackgroundJobErrorService>());
        services.AddSingleton<IAnnouncementRepository, InMemoryAnnouncementRepository>();

        _provider = services.BuildServiceProvider();
        _services = services;
    }

    [Fact]
    public void All_services_can_be_resolved()
    {
        using var scope = _provider.CreateScope();
        foreach (var descriptor in _services.Where(d => d.ServiceType.Namespace?.StartsWith("Predictorator") == true))
        {
            if (descriptor.ServiceType.IsGenericTypeDefinition) continue;
            var service = scope.ServiceProvider.GetService(descriptor.ServiceType);
            Assert.NotNull(service);
        }
    }
}

