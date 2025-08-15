using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;
using Predictorator.Core.Models;
using Predictorator.Core.Services;

namespace Predictorator.Functions;

public class FixtureNotificationsFunction
{
    private readonly NotificationService _service;
    private readonly ILogger<FixtureNotificationsFunction> _logger;
    private readonly IBackgroundJobErrorService _errors;
    private readonly IDateTimeProvider _time;

    public FixtureNotificationsFunction(
        NotificationService service,
        ILogger<FixtureNotificationsFunction> logger,
        IBackgroundJobErrorService errors,
        IDateTimeProvider time)
    {
        _service = service;
        _logger = logger;
        _errors = errors;
        _time = time;
    }

    [Function("FixtureNotifications")]
    public async Task Run([TimerTrigger("%FixtureNotificationsSchedule%")]
        TimerInfo timer)
    {
        try
        {
            _logger.LogInformation("Fixture notifications check started at {Time}", DateTime.UtcNow);
            await _service.CheckFixturesAsync();
            _logger.LogInformation("Fixture notifications check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running fixture notifications");
            await _errors.AddErrorAsync(new BackgroundJobError
            {
                JobId = "FixtureNotifications",
                JobType = "FixtureNotifications",
                Message = ex.Message,
                StackTrace = ex.ToString(),
                OccurredAt = _time.UtcNow
            });
        }
    }
}
