using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;
using Predictorator.Core.Services;

namespace Predictorator.Functions;

public class FixtureNotificationsFunction
{
    private readonly NotificationService _service;
    private readonly ILogger<FixtureNotificationsFunction> _logger;

    public FixtureNotificationsFunction(NotificationService service, ILogger<FixtureNotificationsFunction> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("FixtureNotifications")]
    public async Task Run([TimerTrigger("%FixtureNotificationsSchedule%")]
        TimerInfo timer)
    {
        _logger.LogInformation("Fixture notifications check started at {Time}", DateTime.UtcNow);
        await _service.CheckFixturesAsync();
        _logger.LogInformation("Fixture notifications check completed");
    }
}
