using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Hybrid;
using Predictorator.Models.Fixtures;

namespace Predictorator.Services
{
    public class FixtureService : IFixtureService
    {
        private readonly HttpClient _httpClient;
        private readonly HybridCache _cache;
        private readonly CachePrefixService _prefix;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(12);

        public FixtureService(
            IHttpClientFactory httpClientFactory,
            HybridCache cache,
            CachePrefixService prefix,
            IHttpContextAccessor contextAccessor,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _httpClient = httpClientFactory.CreateClient("fixtures");
            _cache = cache;
            _prefix = prefix;
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _environment = environment;
        }

        public async Task<FixturesResponse> GetFixturesAsync(DateTime fromDate, DateTime toDate)
        {
            var cacheKey = $"{_prefix.Prefix}{fromDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}";

            var options = new HybridCacheEntryOptions { Expiration = _cacheDuration, LocalCacheExpiration = _cacheDuration };

            return await _cache.GetOrCreateAsync(cacheKey, async ct =>
            {
                var token = _configuration["ApiSettings:TestToken"];
                var headerToken = _contextAccessor.HttpContext?.Request.Headers["X-Test-Token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token) && headerToken == token)
                {
                    var path = Path.Combine(_environment.ContentRootPath, "MockData", "fixtures.json");
                    await using var stream = File.OpenRead(path);
                    var mockResponse = await JsonSerializer.DeserializeAsync<FixturesResponse>(stream, cancellationToken: ct);
                    if (mockResponse == null) throw new Exception("Failed to load mock fixtures");
                    return mockResponse;
                }

                var query = new Dictionary<string, string?>
                {
                    ["league"] = "39",
                    ["season"] = fromDate.AddMonths(-7).Year.ToString(),
                    ["from"] = fromDate.ToString("yyyy-MM-dd"),
                    ["to"] = toDate.ToString("yyyy-MM-dd")
                };

                var url = QueryHelpers.AddQueryString("fixtures", query);

                var fetchedResponse = await _httpClient.GetFromJsonAsync<FixturesResponse>(url, cancellationToken: ct);

                if (fetchedResponse == null) throw new Exception("Failed to fetch fixtures");

                return fetchedResponse;
            }, options);
        }
    }
}