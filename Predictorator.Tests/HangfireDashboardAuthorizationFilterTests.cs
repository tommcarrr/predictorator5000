using System.Security.Claims;
using Hangfire.Dashboard;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Predictorator.Authorization;

namespace Predictorator.Tests;

public class HangfireDashboardAuthorizationFilterTests
{
    private class FakeJobStorage : JobStorage
    {
        public override IStorageConnection GetConnection() => throw new NotImplementedException();
        public override IMonitoringApi GetMonitoringApi() => throw new NotImplementedException();
    }

    private static AspNetCoreDashboardContext CreateContext(ClaimsPrincipal user)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var http = new DefaultHttpContext { User = user, RequestServices = services };
        return new AspNetCoreDashboardContext(new FakeJobStorage(), new DashboardOptions(), http);
    }

    [Fact]
    public void Authorize_returns_false_for_anonymous_user()
    {
        var filter = new HangfireDashboardAuthorizationFilter();
        var ctx = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()));

        var result = filter.Authorize(ctx);

        Assert.False(result);
    }

    [Fact]
    public void Authorize_returns_false_for_non_admin_user()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "user") }, "Test");
        var filter = new HangfireDashboardAuthorizationFilter();
        var ctx = CreateContext(new ClaimsPrincipal(identity));

        var result = filter.Authorize(ctx);

        Assert.False(result);
    }

    [Fact]
    public void Authorize_returns_true_for_admin_user()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Admin") }, "Test");
        var filter = new HangfireDashboardAuthorizationFilter();
        var ctx = CreateContext(new ClaimsPrincipal(identity));

        var result = filter.Authorize(ctx);

        Assert.True(result);
    }
}
