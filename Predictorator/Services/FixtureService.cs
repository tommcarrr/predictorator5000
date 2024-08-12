using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace Predictorator.Services
{
    public class FixtureService : IFixtureService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(6);

        public FixtureService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("fixtures");
            _cache = cache;
        }

        public async Task<FixturesResponse> GetFixturesAsync(DateTime fromDate, DateTime toDate)
        {
            var cacheKey = $"{fromDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}";

            if (_cache.TryGetValue(cacheKey, out FixturesResponse? cachedResponse)) return cachedResponse!;
            
            var query = new Dictionary<string, string?>()
            {
                ["league"] = "39",
                ["season"] = "2024",
                ["from"] = fromDate.ToString("yyyy-MM-dd"),
                ["to"] = toDate.ToString("yyyy-MM-dd")
            };

            var url = QueryHelpers.AddQueryString("fixtures", query);

            // Fetch the data from the API
            cachedResponse = await _httpClient.GetFromJsonAsync<FixturesResponse>(url);
            
            if(cachedResponse == null) throw new Exception("Failed to fetch fixtures");

            // Store the data in the cache
            _cache.Set(cacheKey, cachedResponse, _cacheDuration);

            return cachedResponse;
        }
    }
}