using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Predictorator.Endpoints;
using Predictorator.Core.Models;
using Predictorator.Services;

namespace Predictorator.Tests;

public class LoginEndpointsTests
{
    [Fact]
    public async Task Returns_Ok_when_signin_succeeds()
    {
        var service = Substitute.For<ISignInService>();
        service.PasswordSignInAsync("user", "pass").Returns(SignInResult.Success);

        var result = await LoginEndpoints.LoginAsync(new LoginRequest("user", "pass"), service);

        var typed = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, typed.StatusCode);
    }

    [Fact]
    public async Task Returns_BadRequest_when_request_is_null()
    {
        var service = Substitute.For<ISignInService>();

        var result = await LoginEndpoints.LoginAsync(null, service);

        var typed = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, typed.StatusCode);
    }
}
