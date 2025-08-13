using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;
using Predictorator.Core.Services;

namespace Predictorator.Functions;

public class ClearExpiredSubscriptionsFunction
{
    private readonly SubscriptionService _service;
    private readonly ILogger<ClearExpiredSubscriptionsFunction> _logger;

    public ClearExpiredSubscriptionsFunction(SubscriptionService service, ILogger<ClearExpiredSubscriptionsFunction> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("ClearExpiredSubscriptions")]
    public async Task Run([TimerTrigger("%ClearExpiredSubscriptionsSchedule%")]
        TimerInfo timer)
    {
        var count = await _service.ClearExpiredUnverifiedAsync();
        _logger.LogInformation("Removed {Count} expired subscriptions", count);
    }
}
