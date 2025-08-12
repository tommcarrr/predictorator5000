using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Predictorator.Services;

namespace Predictorator.Functions;

public class FixtureNotificationsFunction
{
    private readonly NotificationService _service;

    public FixtureNotificationsFunction(NotificationService service)
    {
        _service = service;
    }

    [Function("FixtureNotifications")]
    public async Task Run([TimerTrigger("%FixtureNotificationsSchedule%")]
        TimerInfo timer)
    {
        await _service.CheckFixturesAsync();
    }
}
