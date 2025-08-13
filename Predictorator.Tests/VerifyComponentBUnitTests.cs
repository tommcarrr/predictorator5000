using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components;
using NSubstitute;
using Predictorator.Components.Pages.Subscription;
using Predictorator.Core.Models;
using Predictorator.Services;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;
using Resend;
using Microsoft.Extensions.Logging.Abstractions;

namespace Predictorator.Tests;

public class VerifyComponentBUnitTests
{
    private BunitContext CreateContext()
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
        ctx.AddBunitPersistentComponentState();

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        ctx.Services.AddSingleton<IConfiguration>(config);
        ctx.Services.AddSingleton<NotificationFeatureService>();

        var store = new InMemoryDataStore();
        ctx.Services.AddSingleton(store);
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
    public async Task Requires_confirmation_before_verifying()
    {
        await using var ctx = CreateContext();
        var store = ctx.Services.GetRequiredService<InMemoryDataStore>();
        await store.AddEmailSubscriberAsync(new Subscriber { Email = "a", IsVerified = false, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow });

        var navMan = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navMan.GetUriWithQueryParameter("token", "v");
        navMan.NavigateTo(uri);
        var cut = ctx.Render<Verify>();

        // Subscriber should not be verified before confirmation
        Assert.False(store.EmailSubscribers.Single().IsVerified);
        Assert.Contains("verify", cut.Markup, StringComparison.OrdinalIgnoreCase);

        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.True(store.EmailSubscribers.Single().IsVerified));
    }
}
