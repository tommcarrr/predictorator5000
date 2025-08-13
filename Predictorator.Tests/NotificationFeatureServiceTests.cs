using Microsoft.Extensions.Configuration;
using Predictorator.Core.Services;

namespace Predictorator.Tests;

public class NotificationFeatureServiceTests
{
    private static NotificationFeatureService CreateService(Dictionary<string, string?>? values = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new())
            .Build();
        return new NotificationFeatureService(config);
    }

    [Fact]
    public void EmailEnabled_true_with_token()
    {
        var svc = CreateService(new Dictionary<string, string?> { ["Resend:ApiToken"] = "token" });
        Assert.True(svc.EmailEnabled);
    }

    [Fact]
    public void EmailEnabled_false_without_token()
    {
        var svc = CreateService();
        Assert.False(svc.EmailEnabled);
    }

    [Fact]
    public void SmsEnabled_true_when_all_settings_present()
    {
        var svc = CreateService(new Dictionary<string, string?>
        {
            ["Twilio:AccountSid"] = "sid",
            ["Twilio:AuthToken"] = "tok",
            ["Twilio:FromNumber"] = "+1"
        });
        Assert.True(svc.SmsEnabled);
    }

    [Fact]
    public void SmsEnabled_false_when_any_setting_missing()
    {
        var svc = CreateService(new Dictionary<string, string?>
        {
            ["Twilio:AccountSid"] = "sid",
            ["Twilio:FromNumber"] = "+1"
        });
        Assert.False(svc.SmsEnabled);
    }

    [Fact]
    public void SubscriptionDisabled_defaults_false()
    {
        var svc = CreateService();
        Assert.False(svc.SubscriptionDisabled);
    }

    [Fact]
    public void SubscriptionDisabledMessage_returns_configured_value()
    {
        var svc = CreateService(new Dictionary<string, string?> { ["Subscription:DisabledMessage"] = "msg" });
        Assert.Equal("msg", svc.SubscriptionDisabledMessage);
    }

    [Fact]
    public void SubscriptionDisabledMessage_returns_default_when_missing()
    {
        var svc = CreateService();
        Assert.Equal(
            "This functionality is temporarily unavailable due to maintenance. Please check back soon.",
            svc.SubscriptionDisabledMessage);
    }

    [Fact]
    public void AnyEnabled_true_when_either_email_or_sms_enabled()
    {
        var svc = CreateService(new Dictionary<string, string?> { ["Resend:ApiToken"] = "token" });
        Assert.True(svc.AnyEnabled);
    }

    [Fact]
    public void AnyEnabled_false_when_all_disabled()
    {
        var svc = CreateService();
        Assert.False(svc.AnyEnabled);
    }
}
