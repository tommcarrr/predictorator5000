using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudBlazor;
using NSubstitute;
using Predictorator.Components.Pages.Subscription;
using Predictorator.Services;
using Predictorator.Tests.Helpers;
using Resend;
using Microsoft.Extensions.Logging.Abstractions;

namespace Predictorator.Tests;

public class SubscribeComponentBUnitTests
{
    private BunitContext CreateContext(Dictionary<string, string?> settings)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var storage = new FakeBrowserStorage();
        ctx.Services.AddSingleton<IBrowserStorage>(storage);
        ctx.Services.AddScoped<ToastInterop>();
        ctx.Services.AddScoped<UiModeService>();
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());

        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ctx.Services.AddSingleton<IConfiguration>(config);
        ctx.Services.AddSingleton<NotificationFeatureService>();

        var store = new InMemoryDataStore();
        var resend = Substitute.For<IResend>();
        var sms = Substitute.For<ITwilioSmsSender>();
        var time = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow, Today = DateTime.Today };
        ctx.Services.AddSingleton<IDateTimeProvider>(time);
        var inliner = new EmailCssInliner();
        var renderer = new EmailTemplateRenderer();
        var logger = NullLogger<SubscriptionService>.Instance;
        ctx.Services.AddSingleton(new SubscriptionService(store, resend, config, sms, time, inliner, renderer, logger));
        return ctx;
    }

    [Fact]
    public async Task Renders_Email_Tab_When_Resend_Configured()
    {
        await using var ctx = CreateContext(new Dictionary<string, string?> { ["Resend:ApiToken"] = "token" });
        var cut = ctx.Render<Subscribe>();
        Assert.Contains("Email address", cut.Markup);
        Assert.DoesNotContain("UK mobile number", cut.Markup);
    }

    [Fact]
    public async Task Renders_Sms_Tab_When_Twilio_Configured()
    {
        await using var ctx = CreateContext(new Dictionary<string, string?>
        {
            ["Twilio:AccountSid"] = "sid",
            ["Twilio:AuthToken"] = "token",
            ["Twilio:FromNumber"] = "+1"
        });
        var cut = ctx.Render<Subscribe>();
        Assert.Contains("UK mobile number", cut.Markup);
        Assert.DoesNotContain("Email address", cut.Markup);
    }

    [Fact]
    public async Task Email_Submit_Does_Not_Require_Phone()
    {
        await using var ctx = CreateContext(new Dictionary<string, string?> { ["Resend:ApiToken"] = "token" });
        var cut = ctx.Render<Subscribe>();
        var input = cut.Find("input");
        input.Change("user@example.com");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("verification link", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Shows_Disabled_Message_When_Disabled()
    {
        await using var ctx = CreateContext(new Dictionary<string, string?>
        {
            ["Subscription:Disabled"] = "true",
            ["Subscription:DisabledMessage"] = "temporarily unavailable"
        });
        var cut = ctx.Render<Subscribe>();
        Assert.Contains("temporarily unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Email address", cut.Markup);
        Assert.DoesNotContain("UK mobile number", cut.Markup);
    }
}
