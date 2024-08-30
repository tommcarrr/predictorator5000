using Predictorator.Middleware;
using Predictorator.Services;

var builder = WebApplication.CreateBuilder(args);

var rapidApiKey = builder.Configuration["ApiSettings:RapidApiKey"];

builder.Services.AddHttpClient("fixtures",client =>
{
    client.BaseAddress = new Uri("https://api-football-v1.p.rapidapi.com/v3/");
    client.DefaultRequestHeaders.Add("x-rapidapi-host", "api-football-v1.p.rapidapi.com");
    client.DefaultRequestHeaders.Add("x-rapidapi-key", rapidApiKey);
});

builder.Services.AddTransient<IFixtureService, FixtureService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseMiddleware<RateLimitingMiddleware>(100, TimeSpan.FromDays(1));

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();