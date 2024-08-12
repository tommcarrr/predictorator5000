namespace Predictorator.Services;

public interface IFixtureService
{
    Task<FixturesResponse> GetFixturesAsync(DateTime fromDate, DateTime toDate);
}