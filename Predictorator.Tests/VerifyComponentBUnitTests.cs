using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components;
using NSubstitute;
using Predictorator.Components.Pages.Subscription;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Services;
using Predictorator.Tests.Helpers;
using Resend;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;

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

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        ctx.Services.AddSingleton(db);
        var store = new EfDataStore(db);
        var resend = Substitute.For<IResend>();
        var sms = Substitute.For<ITwilioSmsSender>();
        var time = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow, Today = DateTime.Today };
        ctx.Services.AddSingleton<IDateTimeProvider>(time);
        var env = new FakeWebHostEnvironment { WebRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) };
        Directory.CreateDirectory(Path.Combine(env.WebRootPath, "css"));
        File.WriteAllText(Path.Combine(env.WebRootPath, "css", "email.css"), "p{color:red;}");
        var inliner = new EmailCssInliner(env);
        var renderer = new EmailTemplateRenderer();
        var logger = NullLogger<SubscriptionService>.Instance;
        ctx.Services.AddSingleton(new SubscriptionService(store, resend, config, sms, time, inliner, renderer, logger));
        return ctx;
    }

    [Fact]
    public async Task Requires_confirmation_before_verifying()
    {
        await using var ctx = CreateContext();
        var db = ctx.Services.GetRequiredService<ApplicationDbContext>();
        db.Subscribers.Add(new Subscriber { Email = "a", IsVerified = false, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var navMan = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navMan.GetUriWithQueryParameter("token", "v");
        navMan.NavigateTo(uri);
        var cut = ctx.Render<Verify>();

        // Subscriber should not be verified before confirmation
        Assert.False(db.Subscribers.Single().IsVerified);
        Assert.Contains("verify", cut.Markup, StringComparison.OrdinalIgnoreCase);

        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.True(db.Subscribers.Single().IsVerified));
    }
}
