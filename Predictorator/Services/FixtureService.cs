using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Text.Json;
using System.Linq;
using Predictorator.Models.Fixtures;

namespace Predictorator.Services
{
    public class FixtureService : IFixtureService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(12);

        public FixtureService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IHttpContextAccessor contextAccessor,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _httpClient = httpClientFactory.CreateClient("fixtures");
            _cache = cache;
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _environment = environment;
        }

        public async Task<FixturesResponse> GetFixturesAsync(DateTime fromDate, DateTime toDate)
        {
            var cacheKey = $"{fromDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}";

            if (_cache.TryGetValue(cacheKey, out FixturesResponse? cachedResponse)) return cachedResponse!;

            var token = _configuration["ApiSettings:TestToken"];
            var headerToken = _contextAccessor.HttpContext?.Request.Headers["X-Test-Token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(token) && headerToken == token)
            {
                var path = Path.Combine(_environment.ContentRootPath, "MockData", "fixtures.json");
                await using var stream = File.OpenRead(path);
                cachedResponse = await JsonSerializer.DeserializeAsync<FixturesResponse>(stream);
                if (cachedResponse == null) throw new Exception("Failed to load mock fixtures");
                _cache.Set(cacheKey, cachedResponse, _cacheDuration);
                return cachedResponse;
            }

            var query = new Dictionary<string, string?>()
            {
                ["league"] = "39",
                ["season"] = fromDate.AddMonths(-7).Year.ToString(),
                ["from"] = fromDate.ToString("yyyy-MM-dd"),
                ["to"] = toDate.ToString("yyyy-MM-dd")
            };

            var url = QueryHelpers.AddQueryString("fixtures", query);

            cachedResponse = await _httpClient.GetFromJsonAsync<FixturesResponse>(url);

            if (cachedResponse == null) throw new Exception("Failed to fetch fixtures");

            _cache.Set(cacheKey, cachedResponse, _cacheDuration);

            return cachedResponse;
        }
    }
}