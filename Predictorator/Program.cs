using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Predictorator.Middleware;
using Predictorator.Services;
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
builder.Services.AddSingleton<IRateLimitService>(sp =>
    new InMemoryRateLimitService(100, TimeSpan.FromDays(1), sp.GetRequiredService<IDateTimeProvider>()));
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
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
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseMiddleware<RateLimitingMiddleware>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

await ApplicationDbInitializer.SeedAdminUserAsync(app.Services);

app.Run();

public partial class Program { }
