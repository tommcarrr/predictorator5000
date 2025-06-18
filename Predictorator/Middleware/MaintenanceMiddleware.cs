using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Predictorator.Middleware;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public MaintenanceMiddleware(RequestDelegate next, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _next = next;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_configuration.GetValue<bool>("Maintenance:Enabled"))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/html; charset=utf-8";
            var file = Path.Combine(_environment.WebRootPath ?? "wwwroot", "maintenance.html");
            await context.Response.SendFileAsync(file);
            return;
        }

        await _next(context);
    }
}
