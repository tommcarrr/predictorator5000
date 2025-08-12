using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;
using Predictorator.Services;

namespace Predictorator.Functions;

public class CheckUnverifiedExpiredFunction
{
    private readonly SubscriptionService _service;
    private readonly ILogger<CheckUnverifiedExpiredFunction> _logger;

    public CheckUnverifiedExpiredFunction(SubscriptionService service, ILogger<CheckUnverifiedExpiredFunction> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("CheckUnverifiedExpired")]
    public async Task Run([TimerTrigger("0 1 * * 1")] TimerInfo timer)
    {
        var count = await _service.CountExpiredUnverifiedAsync();
        _logger.LogInformation("Expired unverified subscriptions: {Count}", count);
    }
}
