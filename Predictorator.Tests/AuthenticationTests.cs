using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace Predictorator.Tests;

public class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact(Skip="Requires table storage connection")]
    public async Task Register_Page_Should_Return_NotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/Identity/Account/Register");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(Skip="Requires table storage connection")]
    public async Task Login_Endpoint_Returns_BadRequest_When_Body_Missing()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/login", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
