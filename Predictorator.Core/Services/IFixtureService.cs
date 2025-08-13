using Predictorator.Core.Models.Fixtures;

namespace Predictorator.Core.Services;

public interface IFixtureService
{
    Task<FixturesResponse> GetFixturesAsync(DateTime fromDate, DateTime toDate);
}