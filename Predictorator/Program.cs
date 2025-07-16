using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Predictorator.Components;
using Hangfire;
using Predictorator.Data;
using Predictorator.Options;
using Predictorator.Services;
using Predictorator.Startup;
using Predictorator.Endpoints;
using Resend;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

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
    Console.Error.WriteLine($"Error {(int)error.Value}: {error.Value}");
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
builder.Services.AddTransient<IFixtureService, FixtureService>();
builder.Services.AddSingleton<IDateRangeCalculator, DateRangeCalculator>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromDays(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddHybridCache();
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
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin";
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
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ISignInService, SignInManagerSignInService>();
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

ToastInterop.Configure(app.Services);


app.UseRateLimiter();

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
    app.UseHangfireDashboard();
    RecurringJob.AddOrUpdate<NotificationService>(
        "fixture-notifications",
        s => s.CheckFixturesAsync(),
        "0 10 * * *",
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")
        });
    RecurringJob.AddOrUpdate<SubscriptionService>(
        "cleanup-unverified",
        service => service.RemoveExpiredUnverifiedAsync(),
        "*/15 * * * *",
        new RecurringJobOptions());
    await ApplicationDbInitializer.SeedAdminUserAsync(app.Services);
}

app.Run();

public partial class Program { }
