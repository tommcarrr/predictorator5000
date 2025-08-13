using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Predictorator.Core.Data;
using Predictorator.Core.Options;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;
using Resend;
using Predictorator.Core.Models;

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
                ["GameWeekCache:CacheDurationHours"] = "1"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        var env = Substitute.For<IHostEnvironment>();
        env.ContentRootPath.Returns(Directory.GetCurrentDirectory());
        services.AddSingleton<IHostEnvironment>(env);

        services.AddHttpClient("fixtures", client =>
        {
            client.BaseAddress = new Uri("https://api-football-v1.p.rapidapi.com/v3/");
        });
        services.AddHttpContextAccessor();
        services.AddTransient<IFixtureService, FixtureService>();
        services.AddSingleton<IDateRangeCalculator, DateRangeCalculator>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<NotificationFeatureService>();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o => o.ApiToken = configuration["Resend:ApiToken"]!);
        services.AddTransient<IResend, ResendClient>();
        services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));
        services.AddTransient<ITwilioSmsSender, TwilioSmsSender>();
        services.AddSingleton<IBackgroundJobService>(_ => Substitute.For<IBackgroundJobService>());
        services.AddSingleton<EmailCssInliner>();
        services.AddSingleton<EmailTemplateRenderer>();
        services.AddTransient<INotificationSender<Subscriber>, EmailNotificationSender>();
        services.AddTransient<INotificationSender<SmsSubscriber>, SmsNotificationSender>();
        services.AddHybridCache();
        services.Configure<GameWeekCacheOptions>(configuration.GetSection(GameWeekCacheOptions.SectionName));
        services.AddSingleton<CachePrefixService>();
        services.AddTransient<IGameWeekService, GameWeekService>();
        var store = new InMemoryDataStore();
        services.AddSingleton<IEmailSubscriberRepository>(store);
        services.AddSingleton<ISmsSubscriberRepository>(store);
        services.AddSingleton<ISentNotificationRepository>(store);
        services.AddSingleton<IGameWeekRepository, InMemoryGameWeekRepository>();
        services.AddTransient<SubscriptionService>();
        services.AddTransient<NotificationService>();

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

