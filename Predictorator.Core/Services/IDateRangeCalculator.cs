namespace Predictorator.Core.Services;

public interface IDateRangeCalculator
{
    (DateTime From, DateTime To) GetDates(DateTime? fromDate, DateTime? toDate, int? weekOffset);
}
