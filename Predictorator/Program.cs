using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Predictorator.Components;
using Predictorator.Startup;
using Predictorator.Endpoints;
using Predictorator.Middleware;
using Predictorator.Data;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.DataProtection;

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
    var minLevel = context.HostingEnvironment.IsProduction() ? LogEventLevel.Warning : LogEventLevel.Information;
    configuration
        .MinimumLevel.Is(minLevel)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.File(Path.Combine(logDir, "app.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
        .WriteTo.Console()
        .WriteTo.AzureApp(restrictedToMinimumLevel: minLevel);
});

var error = StartupValidator.Validate(builder);
if (error.HasValue)
{
    Log.Logger.Fatal("Error {ExitCode}: {ExitCodeName}", (int)error.Value, error.Value);
    Environment.Exit((int)error.Value);
}

builder.Services.AddPredictoratorCore(builder.Configuration);
builder.Services.AddPredictoratorIdentity();
builder.Services.AddPredictoratorUi();


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
}

app.Run();

public partial class Program { }
