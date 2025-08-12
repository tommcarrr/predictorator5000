using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Predictorator.Data;
using Predictorator.Options;
using Predictorator.Services;
using Resend;
using Microsoft.Extensions.Hosting;

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
builder.Services.AddSingleton<EmailCssInliner>();
builder.Services.AddSingleton<EmailTemplateRenderer>();

var connString = configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connString));
    builder.Services.AddScoped<IDataStore, EfDataStore>();
}

builder.Build().Run();
