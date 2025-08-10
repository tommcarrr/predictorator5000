using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Predictorator.Components;
using Hangfire;
using Hangfire.Dashboard;
using Predictorator.Data;
using Predictorator.Options;
using Predictorator.Services;
using Predictorator.Startup;
using Predictorator.Endpoints;
using Predictorator.Middleware;
using Resend;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

var culture = new CultureInfo("en-GB");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
};

builder.Host.UseSerilog((context, services, configuration) =>
{
    var logDir = Path.Combine(context.HostingEnvironment.ContentRootPath, "logs");
    Directory.CreateDirectory(logDir);
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.File(Path.Combine(logDir, "app.log"), rollingInterval: RollingInterval.Day)
        .WriteTo.Console()
        .WriteTo.AzureApp();
});

var error = StartupValidator.Validate(builder);
if (error.HasValue)
{
    Log.Logger.Fatal("Error {ExitCode}: {ExitCodeName}", (int)error.Value, error.Value);
    Environment.Exit((int)error.Value);
}

var rapidApiKey = builder.Configuration["ApiSettings:RapidApiKey"];

builder.Services.AddHttpClient("fixtures", client =>
{
    client.BaseAddress = new Uri("https://api-football-v1.p.rapidapi.com/v3/");
    client.DefaultRequestHeaders.Add("x-rapidapi-host", "api-football-v1.p.rapidapi.com");
    client.DefaultRequestHeaders.Add("x-rapidapi-key", rapidApiKey);
});

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddTransient<IFixtureService, FixtureService>();
builder.Services.AddSingleton<IDateRangeCalculator, DateRangeCalculator>();
builder.Services.AddRouteLimiting(builder.Configuration);
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddHybridCache();
builder.Services.AddSingleton<CachePrefixService>();
builder.Services.Configure<GameWeekCacheOptions>(builder.Configuration.GetSection(GameWeekCacheOptions.SectionName));
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["Resend:ApiToken"]!;
});
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.Configure<TwilioOptions>(builder.Configuration.GetSection(TwilioOptions.SectionName));
builder.Services.AddTransient<ITwilioSmsSender, TwilioSmsSender>();
builder.Services.AddTransient<SubscriptionService>();
builder.Services.AddTransient<NotificationService>();
builder.Services.AddTransient<AdminService>();
builder.Services.AddTransient<IGameWeekService, GameWeekService>();
builder.Services.AddSingleton<EmailCssInliner>();
builder.Services.AddSingleton<EmailTemplateRenderer>();
builder.Services.AddSingleton<NotificationFeatureService>();
builder.Services.Configure<AdminUserOptions>(options =>
{
    builder.Configuration.GetSection(AdminUserOptions.SectionName).Bind(options);
    options.Email = builder.Configuration["ADMIN_EMAIL"] ?? options.Email;
    options.Password = builder.Configuration["ADMIN_PASSWORD"] ?? options.Password;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var efLogger = loggerFactory.CreateLogger("EFCore");
    options
        .UseSqlServer(connectionString)
        .UseLoggerFactory(loggerFactory)
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging()
        .LogTo(message => efLogger.LogError(message), LogLevel.Error);
});
builder.Services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
{
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var efLogger = loggerFactory.CreateLogger("EFCore");
    options
        .UseSqlServer(connectionString)
        .UseLoggerFactory(loggerFactory)
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging()
        .LogTo(message => efLogger.LogError(message), LogLevel.Error);
});
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
});

// Add services to the container.
var razorComponentsBuilder = builder.Services.AddRazorComponents();
razorComponentsBuilder.AddInteractiveServerComponents(options =>
{
    // Enable detailed circuit errors so they surface in the browser.
    options.DetailedErrors = true;
});
builder.Services.AddMudServices();
builder.Services.AddHttpClient();
builder.Services.AddScoped<BrowserInteropService>();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<IBrowserStorage, ProtectedLocalStorageBrowserStorage>();
builder.Services.AddScoped<ToastInterop>();
builder.Services.AddScoped<UiModeService>();
builder.Services.AddScoped<ISignInService, SignInManagerSignInService>();
builder.Services.AddAuthorization();
builder.Services.AddRazorPages();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
    builder.Services.AddHangfireServer();
}

var keyPath = builder.Configuration["DataProtection:KeyPath"];
if (string.IsNullOrWhiteSpace(keyPath))
    keyPath = Path.Combine(builder.Environment.ContentRootPath, "dp-keys");
Directory.CreateDirectory(keyPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
    .SetApplicationName("Predictorator");

var app = builder.Build();


app.UseRequestLocalization(localizationOptions);
app.UseForwardedHeaders();
app.UseRouteLimiting();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

var razorComponents = app.MapRazorComponents<App>();
razorComponents.AddInteractiveServerRenderMode();

app.MapRazorPages();
app.MapPost("/login", LoginEndpoints.LoginAsync);
app.MapGet("/Identity/Account/Register", () => Results.NotFound());
app.MapPost("/Identity/Account/Register", () => Results.NotFound());
app.MapGet("/logout", async (SignInManager<IdentityUser> sm) =>
{
    await sm.SignOutAsync();
    return Results.Redirect("/");
});

if (!app.Environment.IsEnvironment("Testing"))
{
    await ApplicationDbInitializer.SeedAdminUserAsync(app.Services);
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new Predictorator.Authorization.HangfireDashboardAuthorizationFilter() }
    });
    RecurringJob.AddOrUpdate<NotificationService>(
        "fixture-notifications",
        s => s.CheckFixturesAsync(),
        "0 1 * * *",
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")
        });
    RecurringJob.AddOrUpdate<SubscriptionService>(
        "check-unverified-expired",
        service => service.CountExpiredUnverifiedAsync(),
        "0 1 * * 1",
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")
        });
}

app.Run();

public partial class Program { }
