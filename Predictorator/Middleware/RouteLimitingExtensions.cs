using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Predictorator.Options;

namespace Predictorator.Middleware;

public static class RouteLimitingExtensions
{
    public static IServiceCollection AddRouteLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<RouteLimitingOptions>(configuration.GetSection(RouteLimitingOptions.SectionName));
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        return services;
    }

    public static IApplicationBuilder UseRouteLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RouteLimitingMiddleware>();
    }
}
