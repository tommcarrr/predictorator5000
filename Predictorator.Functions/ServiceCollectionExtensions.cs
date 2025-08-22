using System;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Hybrid;
using Predictorator.Core.Data;
using Predictorator.Core.Models;
using Predictorator.Core.Options;
using Predictorator.Core.Services;
using Resend;

namespace Predictorator.Functions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredictoratorFunctionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("fixtures", client =>
        {
            client.BaseAddress = new Uri("https://api-football-v1.p.rapidapi.com/v3/");
            client.DefaultRequestHeaders.Add("x-rapidapi-host", "api-football-v1.p.rapidapi.com");
            var key = configuration["ApiSettings:RapidApiKey"];
            if (!string.IsNullOrWhiteSpace(key))
                client.DefaultRequestHeaders.Add("x-rapidapi-key", key);
        });

        services.AddHttpContextAccessor();
        services.AddTransient<IFixtureService, FixtureService>();
        services.AddSingleton<IDateRangeCalculator, DateRangeCalculator>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<NotificationFeatureService>();

        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o => o.ApiToken = configuration["Resend:ApiToken"] ?? string.Empty);
        services.AddTransient<IResend, ResendClient>();

        services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));
        services.AddTransient<ITwilioSmsSender, TwilioSmsSender>();

        services.AddTransient<SubscriptionService>();
        services.AddTransient<NotificationService>();
        services.AddTransient<AnnouncementService>();
        services.AddTransient<INotificationSender<Subscriber>, EmailNotificationSender>();
        services.AddTransient<INotificationSender<SmsSubscriber>, SmsNotificationSender>();

        services.AddSingleton<IBackgroundJobService, TableBackgroundJobService>();
        services.AddSingleton<IBackgroundJobErrorService, TableBackgroundJobErrorService>();
        services.AddSingleton<EmailCssInliner>();
        services.AddSingleton<EmailTemplateRenderer>();
        services.AddHybridCache();
        services.Configure<GameWeekCacheOptions>(configuration.GetSection(GameWeekCacheOptions.SectionName));
        services.AddSingleton<CachePrefixService>();
        services.AddTransient<IGameWeekService, GameWeekService>();

        var tableConn = configuration.GetConnectionString("TableStorage")
            ?? configuration["TableStorage:ConnectionString"];
        var tableService = new TableServiceClient(tableConn ?? throw new InvalidOperationException("Table storage connection string not configured"));
        services.AddSingleton(tableService);
        services.AddScoped<TableDataStore>();
        services.AddScoped<IEmailSubscriberRepository>(sp => sp.GetRequiredService<TableDataStore>());
        services.AddScoped<ISmsSubscriberRepository>(sp => sp.GetRequiredService<TableDataStore>());
        services.AddScoped<ISentNotificationRepository>(sp => sp.GetRequiredService<TableDataStore>());
        services.AddScoped<IAnnouncementRepository, TableAnnouncementRepository>();
        services.AddScoped<IGameWeekRepository, TableGameWeekRepository>();

        return services;
    }
}

