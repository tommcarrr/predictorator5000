using Predictorator.Core.Models.Fixtures;
using Predictorator.Core.Services;

namespace Predictorator.Tests.Helpers;

public class FakeFixtureService : IFixtureService
{
    private readonly FixturesResponse _response;

    public FakeFixtureService(FixturesResponse response)
    {
        _response = response;
    }

    public Task<FixturesResponse> GetFixturesAsync(DateTime fromDate, DateTime toDate)
    {
        return Task.FromResult(_response);
    }
}
