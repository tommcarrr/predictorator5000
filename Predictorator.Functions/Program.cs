using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Predictorator.Data;
using Predictorator.Options;
using Predictorator.Services;
using Resend;
using Microsoft.Extensions.Hosting;
using Azure.Data.Tables;
using Microsoft.Extensions.Caching.Hybrid;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

var configuration = builder.Configuration;

builder.Services.AddHttpClient("fixtures", client =>
{
    client.BaseAddress = new Uri("https://api-football-v1.p.rapidapi.com/v3/");
    client.DefaultRequestHeaders.Add("x-rapidapi-host", "api-football-v1.p.rapidapi.com");
    var key = configuration["ApiSettings:RapidApiKey"];
    if (!string.IsNullOrWhiteSpace(key))
        client.DefaultRequestHeaders.Add("x-rapidapi-key", key);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IFixtureService, FixtureService>();
builder.Services.AddSingleton<IDateRangeCalculator, DateRangeCalculator>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddSingleton<NotificationFeatureService>();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o => o.ApiToken = configuration["Resend:ApiToken"] ?? string.Empty);
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));
builder.Services.AddTransient<ITwilioSmsSender, TwilioSmsSender>();
builder.Services.AddTransient<SubscriptionService>();
builder.Services.AddTransient<NotificationService>();
builder.Services.AddSingleton<IBackgroundJobService, TableBackgroundJobService>();
builder.Services.AddSingleton<EmailCssInliner>();
builder.Services.AddSingleton<EmailTemplateRenderer>();
builder.Services.AddHybridCache();
builder.Services.Configure<GameWeekCacheOptions>(configuration.GetSection(GameWeekCacheOptions.SectionName));
builder.Services.AddSingleton<CachePrefixService>();
builder.Services.AddTransient<IGameWeekService, GameWeekService>();

var tableConn = configuration.GetConnectionString("TableStorage")
    ?? configuration["TableStorage:ConnectionString"];
var tableService = new TableServiceClient(tableConn ?? throw new InvalidOperationException("Table storage connection string not configured"));
builder.Services.AddSingleton(tableService);
builder.Services.AddScoped<IDataStore, TableDataStore>();
builder.Services.AddScoped<IGameWeekRepository, TableGameWeekRepository>();

builder.Build().Run();
