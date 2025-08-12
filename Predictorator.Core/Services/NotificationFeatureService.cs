namespace Predictorator.Services;

public class NotificationFeatureService
{
    private readonly IConfiguration _config;

    public NotificationFeatureService(IConfiguration config)
    {
        _config = config;
    }

    public bool EmailEnabled => !string.IsNullOrWhiteSpace(_config["Resend:ApiToken"]);

    public bool SmsEnabled =>
        !string.IsNullOrWhiteSpace(_config["Twilio:AccountSid"]) &&
        !string.IsNullOrWhiteSpace(_config["Twilio:AuthToken"]) &&
        !string.IsNullOrWhiteSpace(_config["Twilio:FromNumber"]);

    public bool AnyEnabled => EmailEnabled || SmsEnabled;
}
