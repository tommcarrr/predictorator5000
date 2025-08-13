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
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(12);

        public FixtureService(
            IHttpClientFactory httpClientFactory,
            HybridCache cache,
            CachePrefixService prefix)
        {
            _httpClient = httpClientFactory.CreateClient("fixtures");
            _cache = cache;
            _prefix = prefix;
        }

        public async Task<FixturesResponse> GetFixturesAsync(DateTime fromDate, DateTime toDate)
        {
            var cacheKey = $"{_prefix.Prefix}{fromDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}";

            var options = new HybridCacheEntryOptions { Expiration = _cacheDuration, LocalCacheExpiration = _cacheDuration };

            return await _cache.GetOrCreateAsync(cacheKey, async ct =>
            {
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