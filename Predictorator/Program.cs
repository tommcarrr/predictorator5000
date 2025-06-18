using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Predictorator.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Predictorator.Components;
using MudBlazor.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Resend;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHttpClient<Resend.ResendClient>();
builder.Services.Configure<Resend.ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["Resend:ApiToken"]!;
});
builder.Services.AddTransient<Resend.IResend, Resend.ResendClient>();
builder.Services.AddTransient<SubscriptionService>();

var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "predictorator.db");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!.Replace("%DB_PATH%", dbPath);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllerRoute(
    name: "default",
    pattern: "mvc/{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

if (!app.Environment.IsEnvironment("Testing"))
    await ApplicationDbInitializer.SeedAdminUserAsync(app.Services);

app.Run();

public partial class Program { }
