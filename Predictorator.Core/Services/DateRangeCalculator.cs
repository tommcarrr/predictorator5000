namespace Predictorator.Core.Services;

public class DateRangeCalculator : IDateRangeCalculator
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public DateRangeCalculator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public (DateTime From, DateTime To) GetDates(DateTime? fromDate, DateTime? toDate, int? weekOffset)
    {
        if (fromDate.HasValue && toDate.HasValue)
            return (fromDate.Value, toDate.Value);

        if (fromDate.HasValue)
            return (fromDate.Value, fromDate.Value.AddDays(6));

        if (toDate.HasValue)
            return (toDate.Value.AddDays(-6), toDate.Value);

        var effectiveFrom = _dateTimeProvider.Today;
        while (effectiveFrom.DayOfWeek != DayOfWeek.Tuesday)
        {
            effectiveFrom = effectiveFrom.AddDays(-1);
        }
        effectiveFrom = effectiveFrom.AddDays(3);

        if (weekOffset.HasValue)
        {
            effectiveFrom = effectiveFrom.AddDays(weekOffset.Value * 7);
        }

        var effectiveTo = effectiveFrom.AddDays(6);
        return (effectiveFrom, effectiveTo);
    }
}
