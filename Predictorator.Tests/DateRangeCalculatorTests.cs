using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class DateRangeCalculatorTests
{
    [Fact]
    public void Returns_input_dates_when_provided()
    {
        var provider = new FakeDateTimeProvider { Today = new DateTime(2024,1,1) };
        var calculator = new DateRangeCalculator(provider);

        var from = new DateTime(2024,2,1);
        var to = new DateTime(2024,2,5);

        var (resultFrom, resultTo) = calculator.GetDates(from, to, null);

        Assert.Equal(from, resultFrom);
        Assert.Equal(to, resultTo);
    }

    [Fact]
    public void Calculates_week_based_on_today_and_offset()
    {
        var provider = new FakeDateTimeProvider { Today = new DateTime(2024,4,18) }; // Thursday
        var calculator = new DateRangeCalculator(provider);

        var (resultFrom, resultTo) = calculator.GetDates(null, null, 1);

        // Tuesday of current week is 2024-04-16, +3 => Friday 19th, +7 => 26th
        Assert.Equal(new DateTime(2024,4,26), resultFrom);
        Assert.Equal(new DateTime(2024,5,2), resultTo);
    }
}
