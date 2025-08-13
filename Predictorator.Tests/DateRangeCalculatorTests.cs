using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class DateRangeCalculatorTests
{
    [Fact]
    public void Returns_input_dates_when_provided()
    {
        var provider = new FakeDateTimeProvider { Today = new DateTime(2024, 1, 1) };
        var calculator = new DateRangeCalculator(provider);

        var from = new DateTime(2024, 2, 1);
        var to = new DateTime(2024, 2, 5);

        var (resultFrom, resultTo) = calculator.GetDates(from, to, null);

        Assert.Equal(from, resultFrom);
        Assert.Equal(to, resultTo);
    }

    [Fact]
    public void Calculates_week_based_on_today_and_offset()
    {
        var provider = new FakeDateTimeProvider { Today = new DateTime(2024, 4, 18) }; // Thursday
        var calculator = new DateRangeCalculator(provider);

        var (resultFrom, resultTo) = calculator.GetDates(null, null, 1);

        // Tuesday of current week is 2024-04-16, +3 => Friday 19th, +7 => 26th
        Assert.Equal(new DateTime(2024, 4, 26), resultFrom);
        Assert.Equal(new DateTime(2024, 5, 2), resultTo);
    }

    [Fact]
    public void Uses_from_date_when_only_from_provided()
    {
        var provider = new FakeDateTimeProvider { Today = new DateTime(2024, 1, 1) };
        var calculator = new DateRangeCalculator(provider);

        var from = new DateTime(2024, 3, 1);
        var (resultFrom, resultTo) = calculator.GetDates(from, null, null);

        Assert.Equal(from, resultFrom);
        Assert.Equal(from.AddDays(6), resultTo);
    }

    [Fact]
    public void Uses_to_date_when_only_to_provided()
    {
        var provider = new FakeDateTimeProvider { Today = new DateTime(2024, 1, 1) };
        var calculator = new DateRangeCalculator(provider);

        var to = new DateTime(2024, 3, 7);
        var (resultFrom, resultTo) = calculator.GetDates(null, to, null);

        Assert.Equal(to.AddDays(-6), resultFrom);
        Assert.Equal(to, resultTo);
    }
}
