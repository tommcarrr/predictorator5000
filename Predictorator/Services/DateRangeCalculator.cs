namespace Predictorator.Services;

public class DateRangeCalculator : IDateRangeCalculator
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public DateRangeCalculator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public (DateTime From, DateTime To) GetDates(DateTime? fromDate, DateTime? toDate, int? weekOffset)
    {
        var effectiveFrom = fromDate;
        var effectiveTo = toDate;
        if (effectiveFrom != null && effectiveTo != null)
            return (effectiveFrom.Value, effectiveTo.Value);

        effectiveFrom = _dateTimeProvider.Today;
        while (effectiveFrom.Value.DayOfWeek != DayOfWeek.Tuesday)
        {
            effectiveFrom = effectiveFrom.Value.AddDays(-1);
        }
        effectiveFrom = effectiveFrom.Value.AddDays(3);

        if (weekOffset.HasValue)
        {
            effectiveFrom = effectiveFrom.Value.AddDays(weekOffset.Value * 7);
        }

        effectiveTo = effectiveFrom.Value.AddDays(6);
        return (effectiveFrom.Value, effectiveTo.Value);
    }
}
