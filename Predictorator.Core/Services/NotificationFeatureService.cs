namespace Predictorator.Core.Services;

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

    public bool SubscriptionDisabled => _config.GetValue<bool>("Subscription:Disabled");

    public string SubscriptionDisabledMessage =>
        _config["Subscription:DisabledMessage"] ??
        "This functionality is temporarily unavailable due to maintenance. Please check back soon.";

    public bool AnyEnabled => EmailEnabled || SmsEnabled;
}
