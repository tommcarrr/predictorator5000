using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Predictorator.Data;
using Predictorator.Core.Data;
using Predictorator.Core.Options;
using Predictorator.Services;
using Predictorator.Core.Services;
using Resend;
using MudBlazor.Services;
using System.Threading.RateLimiting;

namespace Predictorator.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredictoratorCore(this IServiceCollection services, IConfiguration configuration)
    {
        var rapidApiKey = configuration["ApiSettings:RapidApiKey"];
        services.AddHttpClient("fixtures", client =>
        {
            client.BaseAddress = new Uri("https://api-football-v1.p.rapidapi.com/v3/");
            client.DefaultRequestHeaders.Add("x-rapidapi-host", "api-football-v1.p.rapidapi.com");
            client.DefaultRequestHeaders.Add("x-rapidapi-key", rapidApiKey);
        });
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        services.AddTransient<IFixtureService, FixtureService>();
        services.AddSingleton<IDateRangeCalculator, DateRangeCalculator>();
        services.Configure<RouteLimitingOptions>(configuration.GetSection(RouteLimitingOptions.SectionName));
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var rateOptions = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                if (rateOptions.ExcludedIPs.Contains(ip))
                {
                    return RateLimitPartition.GetNoLimiter(ip);
                }

                var routeOptions = context.RequestServices.GetRequiredService<IOptions<RouteLimitingOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = routeOptions.UniqueRouteLimit,
                    Window = TimeSpan.FromDays(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });
        });
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddTransient<PredictionProcessingService>();
        services.AddHybridCache();
        services.AddSingleton<CachePrefixService>();
        services.Configure<GameWeekCacheOptions>(configuration.GetSection(GameWeekCacheOptions.SectionName));
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = configuration["Resend:ApiToken"]!;
        });
        services.AddTransient<IResend, ResendClient>();
        services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));
        services.AddTransient<ITwilioSmsSender, TwilioSmsSender>();
        var tableConn = configuration.GetConnectionString("TableStorage")
            ?? configuration["TableStorage:ConnectionString"];
        var tableService = new TableServiceClient(tableConn ?? throw new InvalidOperationException("Table storage connection string not configured"));
        services.AddSingleton(tableService);
        services.AddScoped<TableDataStore>();
        services.AddScoped<IEmailSubscriberRepository>(sp => sp.GetRequiredService<TableDataStore>());
        services.AddScoped<ISmsSubscriberRepository>(sp => sp.GetRequiredService<TableDataStore>());
        services.AddScoped<ISentNotificationRepository>(sp => sp.GetRequiredService<TableDataStore>());
        services.AddScoped<IGameWeekRepository, TableGameWeekRepository>();
        services.AddTransient<SubscriptionService>();
        services.AddTransient<NotificationService>();
        services.AddTransient<AdminService>();
        services.AddTransient<IGameWeekService, GameWeekService>();
        services.AddSingleton<EmailCssInliner>();
        services.AddSingleton<EmailTemplateRenderer>();
        services.AddSingleton<NotificationFeatureService>();
        services.AddSingleton<IBackgroundJobService, TableBackgroundJobService>();
        services.Configure<AdminUserOptions>(options =>
        {
            configuration.GetSection(AdminUserOptions.SectionName).Bind(options);
            options.Email = configuration["ADMIN_EMAIL"] ?? options.Email;
            options.Password = configuration["ADMIN_PASSWORD"] ?? options.Password;
        });
        return services;
    }

    public static IServiceCollection AddPredictoratorIdentity(this IServiceCollection services)
    {
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddDefaultUI()
            .AddDefaultTokenProviders();
        services.AddSingleton<IUserStore<IdentityUser>, InMemoryUserStore>();
        services.AddSingleton<IRoleStore<IdentityRole>, InMemoryRoleStore>();
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
        });
        return services;
    }

    public static IServiceCollection AddPredictoratorUi(this IServiceCollection services)
    {
        var razorComponentsBuilder = services.AddRazorComponents();
        razorComponentsBuilder.AddInteractiveServerComponents(options =>
        {
            options.DetailedErrors = true;
        });
        services.AddMudServices();
        services.AddHttpClient();
        services.AddScoped<BrowserInteropService>();
        services.AddScoped<ProtectedLocalStorage>();
        services.AddScoped<IBrowserStorage, ProtectedLocalStorageBrowserStorage>();
        services.AddScoped<ToastInterop>();
        services.AddScoped<UiModeService>();
        services.AddScoped<ISignInService, SignInManagerSignInService>();
        services.AddAuthorization();
        services.AddRazorPages();
        return services;
    }
}
