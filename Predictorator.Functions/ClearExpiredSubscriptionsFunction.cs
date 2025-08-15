using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;
using Predictorator.Core.Models;
using Predictorator.Core.Services;

namespace Predictorator.Functions;

public class ClearExpiredSubscriptionsFunction
{
    private readonly SubscriptionService _service;
    private readonly ILogger<ClearExpiredSubscriptionsFunction> _logger;
    private readonly IBackgroundJobErrorService _errors;
    private readonly IDateTimeProvider _time;

    public ClearExpiredSubscriptionsFunction(
        SubscriptionService service,
        ILogger<ClearExpiredSubscriptionsFunction> logger,
        IBackgroundJobErrorService errors,
        IDateTimeProvider time)
    {
        _service = service;
        _logger = logger;
        _errors = errors;
        _time = time;
    }

    [Function("ClearExpiredSubscriptions")]
    public async Task Run([TimerTrigger("%ClearExpiredSubscriptionsSchedule%")]
        TimerInfo timer)
    {
        try
        {
            var count = await _service.ClearExpiredUnverifiedAsync();
            _logger.LogInformation("Removed {Count} expired subscriptions", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing expired subscriptions");
            await _errors.AddErrorAsync(new BackgroundJobError
            {
                JobId = "ClearExpiredSubscriptions",
                JobType = "ClearExpiredSubscriptions",
                Message = ex.Message,
                StackTrace = ex.ToString(),
                OccurredAt = _time.UtcNow
            });
        }
    }
}
